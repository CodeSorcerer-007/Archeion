package org.archeion.companion.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import org.archeion.companion.ui.MainViewModel

@Composable
fun CapabilitiesScreen(viewModel: MainViewModel) {
    val capabilities by viewModel.capabilities.collectAsState()
    val scrollState = rememberScrollState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
            .verticalScroll(scrollState),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text("Capability & Storage Audit", style = MaterialTheme.typography.titleLarge)
        Text(
            "Truthful assessment of supported backup categories under Android sandbox policies.",
            style = MaterialTheme.typography.bodyMedium
        )

        capabilities?.let { cap ->
            // Supported Categories
            Text("Preservable Categories", style = MaterialTheme.typography.titleMedium)
            for (cat in cap.supportedCategories) {
                ListItem(
                    headlineContent = { Text(cat) },
                    leadingContent = {
                        Icon(Icons.Default.CheckCircle, contentDescription = null, tint = MaterialTheme.colorScheme.primary)
                    },
                    supportingContent = {
                        Text(when (cat) {
                            "DeviceIdentity" -> "Hardware fingerprint, OEM metadata, OS version"
                            "Photos" -> "Camera DCIM and external images via MediaStore"
                            "Videos" -> "Recorded movies and saved video clips"
                            "Audio" -> "Music, voice recordings, and podcasts"
                            "Contacts" -> "Personal address book via standard vCard (VCF)"
                            "Calendar" -> "Scheduled events and recurring rules via iCalendar (ICS)"
                            "AppInventory" -> "Installed package metadata and extractable base APKs"
                            else -> "Preservable storage category"
                        })
                    }
                )
            }

            HorizontalDivider()

            // Restricted Categories
            Text("Sandbox Restricted Categories", style = MaterialTheme.typography.titleMedium)
            for ((cat, reason) in cap.restrictedCategories) {
                ListItem(
                    headlineContent = { Text(cat) },
                    leadingContent = {
                        Icon(Icons.Default.Warning, contentDescription = null, tint = MaterialTheme.colorScheme.error)
                    },
                    supportingContent = { Text(reason) }
                )
            }
        } ?: CircularProgressIndicator()
    }
}
