package org.archeion.companion.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import org.archeion.companion.network.BridgeState
import org.archeion.companion.ui.MainViewModel

@Composable
fun DashboardScreen(viewModel: MainViewModel) {
    val bridgeState by viewModel.bridgeState.collectAsState()
    val capabilities by viewModel.capabilities.collectAsState()
    val scrollState = rememberScrollState()

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
                    Text("Root Status: ${if (cap.isRooted) "Rooted" else "Standard (Non-root)"}")
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
}
