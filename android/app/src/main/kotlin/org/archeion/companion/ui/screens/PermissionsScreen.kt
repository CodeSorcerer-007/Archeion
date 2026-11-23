package org.archeion.companion.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import org.archeion.companion.ui.MainViewModel

@Composable
fun PermissionsScreen(
    viewModel: MainViewModel,
    onRequestPermissions: () -> Unit
) {
    val capabilities by viewModel.capabilities.collectAsState()
    val scrollState = rememberScrollState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
            .verticalScroll(scrollState),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text("Permission Access", style = MaterialTheme.typography.titleLarge)
        Text(
            "ARCHEION operates entirely offline without root. Access to your personal media and data requires explicit user grant.",
            style = MaterialTheme.typography.bodyMedium
        )

        Button(
            onClick = onRequestPermissions,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Request Missing Permissions")
        }

        capabilities?.grantedPermissions?.forEach { (perm, granted) ->
            ListItem(
                headlineContent = { Text(perm) },
                supportingContent = { Text(if (granted) "Granted" else "Denied or Not Requested") },
                trailingContent = {
                    Icon(
                        imageVector = if (granted) Icons.Default.Check else Icons.Default.Close,
                        contentDescription = null,
                        tint = if (granted) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.error
                    )
                }
            )
        }
    }
}
