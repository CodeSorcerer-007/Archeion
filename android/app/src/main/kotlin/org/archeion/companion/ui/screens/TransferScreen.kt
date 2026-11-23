package org.archeion.companion.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CloudDone
import androidx.compose.material.icons.filled.Sync
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import org.archeion.companion.service.TransferForegroundService
import org.archeion.companion.ui.MainViewModel

@Composable
fun TransferScreen(viewModel: MainViewModel) {
    val transferState by viewModel.transferState.collectAsState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        if (transferState.isActive) {
            Icon(
                imageVector = Icons.Default.Sync,
                contentDescription = null,
                modifier = Modifier.size(64.dp),
                tint = MaterialTheme.colorScheme.primary
            )
            Spacer(modifier = Modifier.height(16.dp))
            Text("Streaming to ARCHEION Desktop", style = MaterialTheme.typography.titleLarge)
            Spacer(modifier = Modifier.height(8.dp))
            Text("Current Category: ${transferState.currentCategory}", style = MaterialTheme.typography.bodyMedium)
            Text("Item: ${transferState.currentItemName.takeLast(35)}", style = MaterialTheme.typography.bodySmall)

            Spacer(modifier = Modifier.height(24.dp))
            LinearProgressIndicator(
                progress = { transferState.progressPercent / 100f },
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(modifier = Modifier.height(8.dp))
            Text("${transferState.progressPercent}% (${transferState.processedItems} items transferred)")

            Spacer(modifier = Modifier.height(32.dp))
            OutlinedButton(
                onClick = { TransferForegroundService.completeTransfer() }
            ) {
                Text("Cancel Transfer")
            }
        } else {
            Icon(
                imageVector = Icons.Default.CloudDone,
                contentDescription = null,
                modifier = Modifier.size(64.dp),
                tint = MaterialTheme.colorScheme.secondary
            )
            Spacer(modifier = Modifier.height(16.dp))
            Text("No Active Transfer", style = MaterialTheme.typography.titleMedium)
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                "When a backup is triggered from ARCHEION Desktop on Windows, live item and chunk progress will appear here.",
                style = MaterialTheme.typography.bodyMedium
            )
        }
    }
}
