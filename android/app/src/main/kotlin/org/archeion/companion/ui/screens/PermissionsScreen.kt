package org.archeion.companion.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Security
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
fun PermissionsScreen(
    viewModel: MainViewModel,
    onRequestPermissions: () -> Unit
) {
    val capabilities by viewModel.capabilities.collectAsState()
    val scrollState = rememberScrollState()

    val allGranted = capabilities?.grantedPermissions?.values?.all { it } == true

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
            .verticalScroll(scrollState),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text("Permission Access", style = MaterialTheme.typography.titleLarge)
        Text(
            "ARCHEION operates entirely offline without root privileges. Preserving personal media and data requires explicit user grant.",
            style = MaterialTheme.typography.bodyMedium
        )

        // Status Banner
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(
                containerColor = if (allGranted) MaterialTheme.colorScheme.primaryContainer else MaterialTheme.colorScheme.tertiaryContainer
            )
        ) {
            Row(
                modifier = Modifier.padding(16.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                Icon(
                    imageVector = if (allGranted) Icons.Default.CheckCircle else Icons.Default.Warning,
                    contentDescription = null,
                    tint = if (allGranted) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.tertiary
                )
                Column {
                    Text(
                        text = if (allGranted) "All Permissions Granted" else "Permissions Review Needed",
                        style = MaterialTheme.typography.titleSmall
                    )
                    Text(
                        text = if (allGranted)
                            "Full non-root preservation available for media, contacts, calendar, and apps."
                        else
                            "Grant missing permissions to allow full data preservation.",
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }
        }

        if (!allGranted) {
            Button(
                onClick = onRequestPermissions,
                modifier = Modifier.fillMaxWidth()
            ) {
                Icon(Icons.Default.Security, contentDescription = null)
                Spacer(Modifier.width(8.dp))
                Text("Request Missing Permissions")
            }
        }

        Text("Permission Ledger", style = MaterialTheme.typography.titleSmall)

        capabilities?.grantedPermissions?.forEach { (perm, granted) ->
            val friendlyName = when {
                perm.contains("READ_MEDIA_IMAGES") -> "Photos & Images"
                perm.contains("READ_MEDIA_VIDEO") -> "Videos"
                perm.contains("READ_MEDIA_AUDIO") -> "Audio & Music"
                perm.contains("READ_CONTACTS") -> "Contacts (Read vCard)"
                perm.contains("WRITE_CONTACTS") -> "Contacts (Restore vCard)"
                perm.contains("READ_CALENDAR") -> "Calendar (Read iCalendar)"
                perm.contains("WRITE_CALENDAR") -> "Calendar (Restore iCalendar)"
                perm.contains("POST_NOTIFICATIONS") -> "Foreground Transfer Notifications"
                perm.contains("READ_EXTERNAL_STORAGE") -> "Legacy Storage Access"
                else -> perm.substringAfterLast('.')
            }

            ElevatedCard(modifier = Modifier.fillMaxWidth()) {
                ListItem(
                    headlineContent = { Text(friendlyName) },
                    supportingContent = { Text(perm) },
                    trailingContent = {
                        AssistChip(
                            onClick = {},
                            label = { Text(if (granted) "Granted" else "Missing") },
                            leadingIcon = {
                                Icon(
                                    imageVector = if (granted) Icons.Default.Check else Icons.Default.Close,
                                    contentDescription = null,
                                    tint = if (granted) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.error,
                                    modifier = Modifier.size(16.dp)
                                )
                            }
                        )
                    }
                )
            }
        }
    }
}
