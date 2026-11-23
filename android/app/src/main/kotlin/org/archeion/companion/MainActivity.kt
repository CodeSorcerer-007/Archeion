package org.archeion.companion

import android.Manifest
import android.content.Intent
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.activity.viewModels
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import org.archeion.companion.service.TransferForegroundService
import org.archeion.companion.ui.MainViewModel
import org.archeion.companion.ui.screens.*
import org.archeion.companion.ui.theme.ArcheionCompanionTheme

enum class Screen(val title: String) {
    Dashboard("Dashboard"),
    Capabilities("Capabilities"),
    Permissions("Permissions"),
    Transfer("Live Transfer"),
    LocalExport("Export & Tools")
}

class MainActivity : ComponentActivity() {

    private val viewModel: MainViewModel by viewModels()

    private val requestPermissionsLauncher = registerForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { _ ->
        viewModel.refreshCapabilities()
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Request initial required permissions
        checkAndRequestPermissions()

        setContent {
            ArcheionCompanionTheme {
                MainAppScaffold(
                    viewModel = viewModel,
                    onRequestPermissions = { checkAndRequestPermissions() }
                )
            }
        }
    }

    private fun checkAndRequestPermissions() {
        val permissions = mutableListOf(
            Manifest.permission.READ_CONTACTS,
            Manifest.permission.WRITE_CONTACTS,
            Manifest.permission.READ_CALENDAR,
            Manifest.permission.WRITE_CALENDAR
        )

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            permissions.add(Manifest.permission.READ_MEDIA_IMAGES)
            permissions.add(Manifest.permission.READ_MEDIA_VIDEO)
            permissions.add(Manifest.permission.READ_MEDIA_AUDIO)
            permissions.add(Manifest.permission.POST_NOTIFICATIONS)
        } else {
            permissions.add(Manifest.permission.READ_EXTERNAL_STORAGE)
        }

        requestPermissionsLauncher.launch(permissions.toTypedArray())
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainAppScaffold(
    viewModel: MainViewModel,
    onRequestPermissions: () -> Unit
) {
    var currentScreen by remember { mutableStateOf(Screen.Dashboard) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("ARCHEION", style = MaterialTheme.typography.titleMedium)
                        Text(currentScreen.title, style = MaterialTheme.typography.bodySmall)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primaryContainer,
                    titleContentColor = MaterialTheme.colorScheme.onPrimaryContainer
                )
            )
        },
        bottomBar = {
            NavigationBar {
                NavigationBarItem(
                    selected = currentScreen == Screen.Dashboard,
                    onClick = { currentScreen = Screen.Dashboard },
                    icon = { Icon(Icons.Default.Dashboard, contentDescription = "Dashboard") },
                    label = { Text("Home") }
                )
                NavigationBarItem(
                    selected = currentScreen == Screen.Capabilities,
                    onClick = { currentScreen = Screen.Capabilities },
                    icon = { Icon(Icons.Default.Info, contentDescription = "Capabilities") },
                    label = { Text("Audit") }
                )
                NavigationBarItem(
                    selected = currentScreen == Screen.Permissions,
                    onClick = { currentScreen = Screen.Permissions },
                    icon = { Icon(Icons.Default.Security, contentDescription = "Permissions") },
                    label = { Text("Access") }
                )
                NavigationBarItem(
                    selected = currentScreen == Screen.Transfer,
                    onClick = { currentScreen = Screen.Transfer },
                    icon = { Icon(Icons.Default.Sync, contentDescription = "Transfer") },
                    label = { Text("Transfer") }
                )
                NavigationBarItem(
                    selected = currentScreen == Screen.LocalExport,
                    onClick = { currentScreen = Screen.LocalExport },
                    icon = { Icon(Icons.Default.FolderZip, contentDescription = "Export") },
                    label = { Text("Export") }
                )
            }
        }
    ) { paddingValues ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            when (currentScreen) {
                Screen.Dashboard -> DashboardScreen(viewModel)
                Screen.Capabilities -> CapabilitiesScreen(viewModel)
                Screen.Permissions -> PermissionsScreen(viewModel, onRequestPermissions)
                Screen.Transfer -> TransferScreen(viewModel)
                Screen.LocalExport -> ExportScreen(viewModel)
            }
        }
    }
}
