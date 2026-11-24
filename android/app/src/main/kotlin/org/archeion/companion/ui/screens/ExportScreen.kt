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
import java.io.FileOutputStream

@Composable
fun ExportScreen(viewModel: MainViewModel) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val scrollState = rememberScrollState()

    var exportStatus by remember { mutableStateOf<String?>(null) }
    var isExporting by remember { mutableStateOf(false) }

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

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
            .verticalScroll(scrollState),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text("Local On-Device Export", style = MaterialTheme.typography.titleLarge)
        Text(
            "Safely preserve your contacts (VCF), calendar events (ICS), and installed app registry (JSON) directly to an SD Card, USB OTG drive, or local storage without a computer.",
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
