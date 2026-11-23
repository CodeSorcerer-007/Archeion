package org.archeion.companion.ui

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import org.archeion.companion.network.BridgeState
import org.archeion.companion.network.DesktopBridgeServer
import org.archeion.companion.protocol.DeviceCapabilityReport
import org.archeion.companion.service.TransferForegroundService
import org.archeion.companion.service.TransferState

class MainViewModel(application: Application) : AndroidViewModel(application) {

    private val bridgeServer = DesktopBridgeServer(application)

    val bridgeState: StateFlow<BridgeState> = bridgeServer.bridgeState
    val transferState: StateFlow<TransferState> = TransferForegroundService.transferState

    private val _capabilities = MutableStateFlow<DeviceCapabilityReport?>(null)
    val capabilities: StateFlow<DeviceCapabilityReport?> = _capabilities.asStateFlow()

    init {
        refreshCapabilities()
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
        }
    }

    override fun onCleared() {
        super.onCleared()
        bridgeServer.stop()
    }
}
