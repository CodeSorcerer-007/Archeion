package org.archeion.companion.ui.screens

import android.content.Intent
import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.filled.Folder
import androidx.compose.material.icons.filled.Security
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.documentfile.provider.DocumentFile
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.archeion.companion.providers.CalendarIcsExporter
import org.archeion.companion.providers.ContactsVcfExporter
import org.archeion.companion.ui.MainViewModel

@Composable
fun ExportScreen(viewModel: MainViewModel) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val scrollState = rememberScrollState()

    var exportStatus by remember { mutableStateOf<String?>(null) }
    var isExporting by remember { mutableStateOf(false) }

    val safDirectoryPicker = rememberLauncherForActivityResult(
        ActivityResultContracts.OpenDocumentTree()
    ) { treeUri: Uri? ->
        if (treeUri != null) {
            isExporting = true
            exportStatus = "Exporting records..."
            scope.launch {
                try {
                    withContext(Dispatchers.IO) {
                        val rootDoc = DocumentFile.fromTreeUri(context, treeUri)
                        val timestamp = System.currentTimeMillis()
                        val archeionDir = rootDoc?.createDirectory("ARCHEION_EXPORT_$timestamp")

                        // 1. Export Contacts VCF
                        val contactsExporter = ContactsVcfExporter(context)
                        val vcfBytes = contactsExporter.exportToVcf()
                        val vcfFile = archeionDir?.createFile("text/x-vcard", "contacts.vcf")
                        vcfFile?.uri?.let { uri ->
                            context.contentResolver.openOutputStream(uri)?.use { out ->
                                out.write(vcfBytes)
                            }
                        }

                        // 2. Export Calendar ICS
                        val calendarExporter = CalendarIcsExporter(context)
                        val icsBytes = calendarExporter.exportToIcs()
                        val icsFile = archeionDir?.createFile("text/calendar", "calendar.ics")
                        icsFile?.uri?.let { uri ->
                            context.contentResolver.openOutputStream(uri)?.use { out ->
                                out.write(icsBytes)
                            }
                        }
                    }
                    exportStatus = "Export successfully completed to selected folder!"
                } catch (e: Exception) {
                    exportStatus = "Export failed: ${e.localizedMessage}"
                } finally {
                    isExporting = false
                }
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
            "Safely preserve your contacts (VCF) and calendar events (ICS) directly to an SD Card, USB OTG drive, or local storage without a computer.",
            style = MaterialTheme.typography.bodyMedium
        )

        ElevatedCard(modifier = Modifier.fillMaxWidth()) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text("Standard Open Formats", style = MaterialTheme.typography.titleMedium)
                Spacer(modifier = Modifier.height(4.dp))
                Text("• Contacts: RFC 6350 / vCard 3.0 (.vcf)")
                Text("• Calendar: RFC 5545 / iCalendar (.ics)")
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
