package org.archeion.companion.ui.screens

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import org.archeion.companion.network.BridgeState
import org.archeion.companion.ui.MainViewModel

@Composable
fun DashboardScreen(viewModel: MainViewModel) {
    val bridgeState by viewModel.bridgeState.collectAsState()
    val capabilities by viewModel.capabilities.collectAsState()
    val inventory by viewModel.inventory.collectAsState()
    val scrollState = rememberScrollState()

    var selectedCategoryInfo by remember { mutableStateOf<Pair<String, String>?>(null) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
            .verticalScroll(scrollState),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Status Card
        ElevatedCard(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.elevatedCardColors(
                containerColor = when (bridgeState) {
                    BridgeState.Transferring -> MaterialTheme.colorScheme.primaryContainer
                    BridgeState.Connected -> MaterialTheme.colorScheme.secondaryContainer
                    BridgeState.Listening -> MaterialTheme.colorScheme.surfaceVariant
                    BridgeState.Stopped -> MaterialTheme.colorScheme.errorContainer
                }
            )
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Icon(
                        imageVector = when (bridgeState) {
                            BridgeState.Transferring -> Icons.Default.Sync
                            BridgeState.Connected -> Icons.Default.Cable
                            BridgeState.Listening -> Icons.Default.Sensors
                            BridgeState.Stopped -> Icons.Default.CloudOff
                        },
                        contentDescription = null
                    )
                    Text(
                        text = "Desktop Bridge: ${bridgeState.name.uppercase()}",
                        style = MaterialTheme.typography.titleMedium
                    )
                }
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = when (bridgeState) {
                        BridgeState.Transferring -> "Actively streaming data chunks to ARCHEION Desktop."
                        BridgeState.Connected -> "Connected to ARCHEION Desktop on port 8742 (ADB reverse ready)."
                        BridgeState.Listening -> "Listening on 127.0.0.1:8742. Connect via USB (ADB) or local Wi-Fi."
                        BridgeState.Stopped -> "Bridge service is offline."
                    },
                    style = MaterialTheme.typography.bodyMedium
                )
                if (bridgeState == BridgeState.Listening || bridgeState == BridgeState.Connected) {
                    Spacer(modifier = Modifier.height(8.dp))
                    AssistChip(
                        onClick = {},
                        label = { Text("mDNS LAN Auto-Discovery: _archeion._tcp") },
                        leadingIcon = {
                            Icon(Icons.Default.Wifi, contentDescription = null, modifier = Modifier.size(16.dp))
                        }
                    )
                }
            }
        }

        // Category Inventory Card
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text("Preservable Storage Inventory", style = MaterialTheme.typography.titleSmall)
                    if (inventory.isScanning) {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp), strokeWidth = 2.dp)
                    }
                }
                Spacer(modifier = Modifier.height(4.dp))
                Text("Tap any category to inspect preservation details", style = MaterialTheme.typography.labelSmall)
                Spacer(modifier = Modifier.height(12.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    InventoryStatItem("Photos", inventory.photosCount, Icons.Default.PhotoLibrary) {
                        selectedCategoryInfo = "Photos & Images" to "${inventory.photosCount} photos discovered in DCIM/Camera via MediaStore.\n\nPreserved in full resolution with original EXIF metadata intact."
                    }
                    InventoryStatItem("Videos", inventory.videosCount, Icons.Default.VideoLibrary) {
                        selectedCategoryInfo = "Videos" to "${inventory.videosCount} video recordings discovered via MediaStore.\n\nPreserved without lossy transcoding or recompression."
                    }
                    InventoryStatItem("Audio", inventory.audioCount, Icons.Default.Audiotrack) {
                        selectedCategoryInfo = "Audio & Music" to "${inventory.audioCount} audio files discovered.\n\nIncludes voice recordings, downloaded music, and podcasts."
                    }
                }
                Spacer(modifier = Modifier.height(12.dp))
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    InventoryStatItem("Contacts", inventory.contactsCount, Icons.Default.Contacts) {
                        selectedCategoryInfo = "Contacts" to "${inventory.contactsCount} contacts indexed.\n\nPreserved via RFC 6350 / vCard 3.0 standard with phones, emails, and postal addresses."
                    }
                    InventoryStatItem("Calendar", inventory.calendarEventsCount, Icons.Default.CalendarMonth) {
                        selectedCategoryInfo = "Calendar Events" to "${inventory.calendarEventsCount} calendar events indexed.\n\nPreserved via RFC 5545 / iCalendar 2.0 with UTC event timestamps and recurrence rules."
                    }
                    InventoryStatItem("Apps", inventory.installedAppsCount, Icons.Default.Apps) {
                        selectedCategoryInfo = "Installed Applications" to "${inventory.installedAppsCount} installed packages cataloged.\n\nPreserves complete application registry (package name, version codes, SDK targets) and extractable base APKs."
                    }
                }
            }
        }

        // Device Info Card
        OutlinedCard(modifier = Modifier.fillMaxWidth()) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text("Device Identity", style = MaterialTheme.typography.titleSmall)
                Spacer(modifier = Modifier.height(8.dp))
                capabilities?.let { cap ->
                    Text("Model: ${cap.manufacturer} ${cap.model}")
                    Text("Android: ${cap.androidVersion} (API ${cap.apiLevel})")
                    Text("Root Status: ${if (cap.isRooted) "Rooted" else "Standard Non-Root (Sandbox Enforced)"}")
                    Text("Build: ${cap.buildFingerprint.take(35)}...")
                } ?: Text("Querying device hardware identity...")
            }
        }

        // Quick Controls
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            if (bridgeState == BridgeState.Stopped) {
                Button(
                    onClick = { viewModel.startServer() },
                    modifier = Modifier.weight(1f)
                ) {
                    Icon(Icons.Default.PlayArrow, contentDescription = null)
                    Spacer(Modifier.width(8.dp))
                    Text("Start Bridge")
                }
            } else {
                OutlinedButton(
                    onClick = { viewModel.stopServer() },
                    modifier = Modifier.weight(1f)
                ) {
                    Icon(Icons.Default.Stop, contentDescription = null)
                    Spacer(Modifier.width(8.dp))
                    Text("Stop Bridge")
                }
            }

            FilledTonalButton(
                onClick = { viewModel.refreshCapabilities() },
                modifier = Modifier.weight(1f)
            ) {
                Icon(Icons.Default.Refresh, contentDescription = null)
                Spacer(Modifier.width(8.dp))
                Text("Refresh Audit")
            }
        }
    }

    // Detail dialog when an inventory item is tapped
    selectedCategoryInfo?.let { (title, description) ->
        AlertDialog(
            onDismissRequest = { selectedCategoryInfo = null },
            icon = { Icon(Icons.Default.Info, contentDescription = null) },
            title = { Text(title) },
            text = { Text(description) },
            confirmButton = {
                TextButton(onClick = { selectedCategoryInfo = null }) {
                    Text("Close")
                }
            }
        )
    }
}

@Composable
private fun InventoryStatItem(
    title: String,
    count: Int,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    onClick: () -> Unit = {}
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = Modifier
            .width(90.dp)
            .clickable { onClick() }
            .padding(vertical = 4.dp)
    ) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.primary,
            modifier = Modifier.size(24.dp)
        )
        Spacer(modifier = Modifier.height(4.dp))
        Text(
            text = count.toString(),
            style = MaterialTheme.typography.titleMedium
        )
        Text(
            text = title,
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}
