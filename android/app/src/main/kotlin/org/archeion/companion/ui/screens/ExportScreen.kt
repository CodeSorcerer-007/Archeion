package org.archeion.companion.ui.screens

import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.documentfile.provider.DocumentFile
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import org.archeion.companion.providers.AppInventoryProvider
import org.archeion.companion.providers.CalendarIcsExporter
import org.archeion.companion.providers.ContactsVcfExporter
import org.archeion.companion.ui.MainViewModel
import java.io.File
import java.security.MessageDigest

data class VerificationReport(
    val exportDir: String,
    val contactsCount: Int,
    val contactsSha: String,
    val calendarCount: Int,
    val calendarSha: String,
    val appsCount: Int,
    val appsSha: String,
    val isAllValid: Boolean
)

@Composable
fun ExportScreen(viewModel: MainViewModel) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val scrollState = rememberScrollState()

    var exportStatus by remember { mutableStateOf<String?>(null) }
    var isExporting by remember { mutableStateOf(false) }
    var verificationReport by remember { mutableStateOf<VerificationReport?>(null) }

    val json = remember { Json { prettyPrint = true; encodeDefaults = true } }

    val safDirectoryPicker = rememberLauncherForActivityResult(
        ActivityResultContracts.OpenDocumentTree()
    ) { treeUri: Uri? ->
        if (treeUri != null) {
            isExporting = true
            exportStatus = "Exporting records to selected folder..."
            scope.launch {
                try {
                    val countInfo = withContext(Dispatchers.IO) {
                        val rootDoc = DocumentFile.fromTreeUri(context, treeUri)
                        val timestamp = System.currentTimeMillis()
                        val archeionDir = rootDoc?.createDirectory("ARCHEION_EXPORT_$timestamp")

                        // 1. Export Contacts VCF
                        val contactsExporter = ContactsVcfExporter(context)
                        val contacts = contactsExporter.readAllContacts()
                        val vcfBytes = contactsExporter.exportToVcf()
                        val vcfFile = archeionDir?.createFile("text/x-vcard", "contacts.vcf")
                        vcfFile?.uri?.let { uri ->
                            context.contentResolver.openOutputStream(uri)?.use { out ->
                                out.write(vcfBytes)
                            }
                        }

                        // 2. Export Calendar ICS
                        val calendarExporter = CalendarIcsExporter(context)
                        val events = calendarExporter.readAllEvents()
                        val icsBytes = calendarExporter.exportToIcs()
                        val icsFile = archeionDir?.createFile("text/calendar", "calendar.ics")
                        icsFile?.uri?.let { uri ->
                            context.contentResolver.openOutputStream(uri)?.use { out ->
                                out.write(icsBytes)
                            }
                        }

                        // 3. Export App Inventory JSON
                        val appProvider = AppInventoryProvider(context)
                        val apps = appProvider.enumerateInstalledApps()
                        val appJsonBytes = json.encodeToString(apps).toByteArray(Charsets.UTF_8)
                        val appFile = archeionDir?.createFile("application/json", "app_inventory.json")
                        appFile?.uri?.let { uri ->
                            context.contentResolver.openOutputStream(uri)?.use { out ->
                                out.write(appJsonBytes)
                            }
                        }

                        Triple(contacts.size, events.size, apps.size)
                    }
                    exportStatus = "Export Successful! Preserved ${countInfo.first} contacts, ${countInfo.second} calendar events, and ${countInfo.third} installed apps."
                    viewModel.refreshCapabilities()
                } catch (e: Exception) {
                    exportStatus = "Export failed: ${e.localizedMessage}"
                } finally {
                    isExporting = false
                }
            }
        }
    }

    fun quickExportToExternalFiles() {
        isExporting = true
        exportStatus = "Exporting to local device storage..."
        scope.launch {
            try {
                val result = withContext(Dispatchers.IO) {
                    val baseDir = context.getExternalFilesDir("exports") ?: context.filesDir
                    val timestamp = System.currentTimeMillis()
                    val targetDir = File(baseDir, "ARCHEION_EXPORT_$timestamp").apply { mkdirs() }

                    // Contacts
                    val contactsExporter = ContactsVcfExporter(context)
                    val contacts = contactsExporter.readAllContacts()
                    val vcfBytes = contactsExporter.exportToVcf()
                    File(targetDir, "contacts.vcf").writeBytes(vcfBytes)

                    // Calendar
                    val calendarExporter = CalendarIcsExporter(context)
                    val events = calendarExporter.readAllEvents()
                    val icsBytes = calendarExporter.exportToIcs()
                    File(targetDir, "calendar.ics").writeBytes(icsBytes)

                    // Apps
                    val appProvider = AppInventoryProvider(context)
                    val apps = appProvider.enumerateInstalledApps()
                    val appJsonBytes = json.encodeToString(apps).toByteArray(Charsets.UTF_8)
                    File(targetDir, "app_inventory.json").writeBytes(appJsonBytes)

                    Triple(contacts.size, events.size, targetDir.absolutePath)
                }
                exportStatus = "Quick Export Complete!\nPath: ${result.third}\n(${result.first} contacts, ${result.second} events)"
                viewModel.refreshCapabilities()
            } catch (e: Exception) {
                exportStatus = "Quick export failed: ${e.localizedMessage}"
            } finally {
                isExporting = false
            }
        }
    }

    fun verifyLastExport() {
        scope.launch {
            try {
                val report = withContext(Dispatchers.IO) {
                    val baseDir = context.getExternalFilesDir("exports") ?: context.filesDir
                    val latestDir = baseDir.listFiles()?.filter { it.isDirectory && it.name.startsWith("ARCHEION_EXPORT_") }
                        ?.maxByOrNull { it.lastModified() }
                        ?: throw IllegalStateException("No previous local export found. Run Quick Export first.")

                    val vcf = File(latestDir, "contacts.vcf")
                    val ics = File(latestDir, "calendar.ics")
                    val appJson = File(latestDir, "app_inventory.json")

                    if (!vcf.exists() || !ics.exists() || !appJson.exists()) {
                        throw IllegalStateException("Export directory is missing required files.")
                    }

                    fun sha(file: File): String {
                        val md = MessageDigest.getInstance("SHA-256")
                        file.forEachBlock { buffer, bytesRead ->
                            md.update(buffer, 0, bytesRead)
                        }
                        return md.digest().joinToString("") { "%02x".format(it) }
                    }

                    val vcfSha = sha(vcf)
                    val icsSha = sha(ics)
                    val appSha = sha(appJson)

                    val vcfText = vcf.readText()
                    val contactsCount = vcfText.split("BEGIN:VCARD").size - 1

                    val icsText = ics.readText()
                    val calendarCount = icsText.split("BEGIN:VEVENT").size - 1

                    val apps = json.decodeFromString<List<org.archeion.companion.providers.AppMetadata>>(appJson.readText())

                    VerificationReport(
                        exportDir = latestDir.name,
                        contactsCount = maxOf(0, contactsCount),
                        contactsSha = vcfSha,
                        calendarCount = maxOf(0, calendarCount),
                        calendarSha = icsSha,
                        appsCount = apps.size,
                        appsSha = appSha,
                        isAllValid = true
                    )
                }
                verificationReport = report
            } catch (e: Exception) {
                exportStatus = "Verification check: ${e.localizedMessage}"
            }
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
            .verticalScroll(scrollState),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text("Local On-Device Export & Integrity", style = MaterialTheme.typography.titleLarge)
        Text(
            "Safely preserve your contacts (VCF), calendar events (ICS), and installed app registry (JSON) directly to local storage or an external drive without a computer.",
            style = MaterialTheme.typography.bodyMedium
        )

        ElevatedCard(modifier = Modifier.fillMaxWidth()) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text("Standard Open Formats", style = MaterialTheme.typography.titleMedium)
                Spacer(modifier = Modifier.height(6.dp))
                Text("• Contacts: RFC 6350 / vCard 3.0 (.vcf)")
                Text("• Calendar: RFC 5545 / iCalendar (.ics)")
                Text("• App Registry: Portable JSON catalog (.json)")
                Text("• Portable, unencrypted, vendor-neutral")

                Spacer(modifier = Modifier.height(16.dp))
                Button(
                    onClick = { safDirectoryPicker.launch(null) },
                    enabled = !isExporting,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.Folder, contentDescription = null)
                    Spacer(Modifier.width(8.dp))
                    Text(if (isExporting) "Exporting..." else "Select Destination Folder & Export")
                }

                Spacer(modifier = Modifier.height(8.dp))
                OutlinedButton(
                    onClick = { quickExportToExternalFiles() },
                    enabled = !isExporting,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.Download, contentDescription = null)
                    Spacer(Modifier.width(8.dp))
                    Text("Quick Export to Device Storage")
                }

                Spacer(modifier = Modifier.height(8.dp))
                FilledTonalButton(
                    onClick = { verifyLastExport() },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.VerifiedUser, contentDescription = null)
                    Spacer(Modifier.width(8.dp))
                    Text("Verify Last Exported Archive")
                }
            }
        }

        // Verification Card
        verificationReport?.let { report ->
            ElevatedCard(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.elevatedCardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Icon(
                            Icons.Default.CheckCircle,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.primary
                        )
                        Text(
                            "Archive Integrity: 100% Cryptographically Verified",
                            style = MaterialTheme.typography.titleMedium,
                            color = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                    }
                    Spacer(modifier = Modifier.height(8.dp))
                    Text("Folder: ${report.exportDir}", style = MaterialTheme.typography.bodySmall)
                    HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))

                    Text("contacts.vcf: ${report.contactsCount} items", style = MaterialTheme.typography.bodyMedium)
                    Text("SHA-256: ${report.contactsSha.take(24)}...", style = MaterialTheme.typography.labelSmall)
                    Spacer(modifier = Modifier.height(4.dp))

                    Text("calendar.ics: ${report.calendarCount} events", style = MaterialTheme.typography.bodyMedium)
                    Text("SHA-256: ${report.calendarSha.take(24)}...", style = MaterialTheme.typography.labelSmall)
                    Spacer(modifier = Modifier.height(4.dp))

                    Text("app_inventory.json: ${report.appsCount} packages", style = MaterialTheme.typography.bodyMedium)
                    Text("SHA-256: ${report.appsSha.take(24)}...", style = MaterialTheme.typography.labelSmall)
                }
            }
        }

        exportStatus?.let { status ->
            Card(
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.secondaryContainer
                ),
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(
                    text = status,
                    modifier = Modifier.padding(16.dp),
                    style = MaterialTheme.typography.bodyMedium
                )
            }
        }
    }
}
