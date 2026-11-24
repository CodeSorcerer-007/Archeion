package org.archeion.companion.ui

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import org.archeion.companion.network.BridgeState
import org.archeion.companion.network.DesktopBridgeServer
import org.archeion.companion.protocol.DeviceCapabilityReport
import org.archeion.companion.providers.AppInventoryProvider
import org.archeion.companion.providers.CalendarIcsExporter
import org.archeion.companion.providers.ContactsVcfExporter
import org.archeion.companion.providers.MediaStoreProvider
import org.archeion.companion.service.TransferForegroundService
import org.archeion.companion.service.TransferState

data class CategoryInventory(
    val photosCount: Int = 0,
    val videosCount: Int = 0,
    val audioCount: Int = 0,
    val contactsCount: Int = 0,
    val calendarEventsCount: Int = 0,
    val installedAppsCount: Int = 0,
    val isScanning: Boolean = false
)

class MainViewModel(application: Application) : AndroidViewModel(application) {

    private val bridgeServer = DesktopBridgeServer(application)
    private val mediaStoreProvider = MediaStoreProvider(application)
    private val contactsExporter = ContactsVcfExporter(application)
    private val calendarExporter = CalendarIcsExporter(application)
    private val appInventoryProvider = AppInventoryProvider(application)

    val bridgeState: StateFlow<BridgeState> = bridgeServer.bridgeState
    val transferState: StateFlow<TransferState> = TransferForegroundService.transferState

    private val _capabilities = MutableStateFlow<DeviceCapabilityReport?>(null)
    val capabilities: StateFlow<DeviceCapabilityReport?> = _capabilities.asStateFlow()

    private val _inventory = MutableStateFlow(CategoryInventory())
    val inventory: StateFlow<CategoryInventory> = _inventory.asStateFlow()

    init {
        refreshCapabilities()
        refreshInventory()
        startServer()
    }

    fun startServer() {
        bridgeServer.start()
    }

    fun stopServer() {
        bridgeServer.stop()
    }

    fun refreshCapabilities() {
        viewModelScope.launch {
            _capabilities.value = bridgeServer.buildCapabilityReport()
            refreshInventory()
        }
    }

    fun refreshInventory() {
        viewModelScope.launch(Dispatchers.IO) {
            _inventory.value = _inventory.value.copy(isScanning = true)
            val photos = try { mediaStoreProvider.queryImages().size } catch (e: Exception) { 0 }
            val videos = try { mediaStoreProvider.queryVideos().size } catch (e: Exception) { 0 }
            val audio = try { mediaStoreProvider.queryAudio().size } catch (e: Exception) { 0 }
            val contacts = try { contactsExporter.readAllContacts().size } catch (e: Exception) { 0 }
            val calendar = try { calendarExporter.readAllEvents().size } catch (e: Exception) { 0 }
            val apps = try { appInventoryProvider.enumerateInstalledApps().size } catch (e: Exception) { 0 }

            _inventory.value = CategoryInventory(
                photosCount = photos,
                videosCount = videos,
                audioCount = audio,
                contactsCount = contacts,
                calendarEventsCount = calendar,
                installedAppsCount = apps,
                isScanning = false
            )
        }
    }

    override fun onCleared() {
        super.onCleared()
        bridgeServer.stop()
    }
}
