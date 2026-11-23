package org.archeion.companion.network

import android.content.Context
import android.os.Build
import androidx.core.content.ContextCompat
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import org.archeion.companion.protocol.*
import org.archeion.companion.providers.AppInventoryProvider
import org.archeion.companion.providers.CalendarIcsExporter
import org.archeion.companion.providers.ContactsVcfExporter
import org.archeion.companion.providers.MediaStoreProvider
import org.archeion.companion.service.TransferForegroundService
import java.io.InputStream
import java.io.OutputStream
import java.net.ServerSocket
import java.net.Socket
import java.security.MessageDigest
import java.util.UUID

enum class BridgeState {
    Stopped,
    Listening,
    Connected,
    Transferring
}

class DesktopBridgeServer(
    private val context: Context,
    private val port: Int = 8742
) {
    private val scope = CoroutineScope(Dispatchers.IO + Job())
    private var serverSocket: ServerSocket? = null
    private var serverJob: Job? = null

    private val json = Json { ignoreUnknownKeys = true; encodeDefaults = true }

    private val _bridgeState = MutableStateFlow(BridgeState.Stopped)
    val bridgeState: StateFlow<BridgeState> = _bridgeState.asStateFlow()

    private val mediaProvider = MediaStoreProvider(context)
    private val contactsExporter = ContactsVcfExporter(context)
    private val calendarExporter = CalendarIcsExporter(context)
    private val appProvider = AppInventoryProvider(context)

    fun start() {
        if (serverJob != null && serverJob?.isActive == true) return

        serverJob = scope.launch {
            try {
                serverSocket = ServerSocket(port)
                _bridgeState.value = BridgeState.Listening

                while (isActive) {
                    val clientSocket = serverSocket?.accept() ?: break
                    _bridgeState.value = BridgeState.Connected
                    handleClientConnection(clientSocket)
                    _bridgeState.value = BridgeState.Listening
                }
            } catch (e: Exception) {
                _bridgeState.value = BridgeState.Stopped
            }
        }
    }

    fun stop() {
        try {
            serverSocket?.close()
            serverJob?.cancel()
        } catch (_: Exception) {}
        _bridgeState.value = BridgeState.Stopped
    }

    private fun handleClientConnection(socket: Socket) {
        socket.use { s ->
            val input = s.getInputStream()
            val output = s.getOutputStream()

            var sessionToken: String? = null

            try {
                while (scope.isActive && !s.isClosed) {
                    val (header, payload) = ProtocolFraming.readPacket(input)
                    when (header.messageType) {
                        MessageTypes.HANDSHAKE_REQUEST -> {
                            sessionToken = UUID.randomUUID().toString()
                            val response = HandshakeResponse(
                                accepted = true,
                                sessionToken = sessionToken,
                                deviceFingerprint = Build.FINGERPRINT
                            )
                            val jsonBytes = json.encodeToString(response).toByteArray(Charsets.UTF_8)
                            ProtocolFraming.writePacket(output, MessageTypes.HANDSHAKE_RESPONSE, jsonBytes, header.sequenceNumber)
                        }

                        MessageTypes.CAPABILITY_REPORT -> {
                            val report = buildCapabilityReport()
                            val jsonBytes = json.encodeToString(report).toByteArray(Charsets.UTF_8)
                            ProtocolFraming.writePacket(output, MessageTypes.CAPABILITY_REPORT, jsonBytes, header.sequenceNumber)
                        }

                        MessageTypes.START_BACKUP -> {
                            val request = json.decodeFromString<StartBackupRequest>(String(payload, Charsets.UTF_8))
                            _bridgeState.value = BridgeState.Transferring
                            executeBackupStreaming(output, input, request.requestedCategories)
                            _bridgeState.value = BridgeState.Connected
                        }

                        MessageTypes.SESSION_COMPLETE -> {
                            TransferForegroundService.completeTransfer()
                            break
                        }
                    }
                }
            } catch (e: Exception) {
                TransferForegroundService.completeTransfer()
            }
        }
    }

    private fun executeBackupStreaming(output: OutputStream, input: InputStream, categories: List<String>) {
        val tasks = mutableListOf<Triple<String, String, () -> InputStream?>>()

        if (categories.contains("Contacts")) {
            tasks.add(Triple("contacts.vcf", "Contacts") { contactsExporter.exportToVcf().inputStream() })
        }
        if (categories.contains("Calendar")) {
            tasks.add(Triple("calendar.ics", "Calendar") { calendarExporter.exportToIcs().inputStream() })
        }
        if (categories.contains("Photos")) {
            for (item in mediaProvider.queryImages()) {
                tasks.add(Triple(item.logicalPath, item.category) { mediaProvider.openInputStream(item) })
            }
        }
        if (categories.contains("Videos")) {
            for (item in mediaProvider.queryVideos()) {
                tasks.add(Triple(item.logicalPath, item.category) { mediaProvider.openInputStream(item) })
            }
        }
        if (categories.contains("Audio")) {
            for (item in mediaProvider.queryAudio()) {
                tasks.add(Triple(item.logicalPath, item.category) { mediaProvider.openInputStream(item) })
            }
        }
        if (categories.contains("AppInventory")) {
            val apps = appProvider.enumerateInstalledApps()
            val appsJson = json.encodeToString(apps)
            tasks.add(Triple("app_inventory.json", "AppInventory") { appsJson.toByteArray(Charsets.UTF_8).inputStream() })
        }

        var processedItems = 0L
        val totalItems = tasks.size.toLong()
        var processedBytes = 0L

        for (task in tasks) {
            val (path, category, streamSupplier) = task
            val stream = streamSupplier() ?: continue
            val dataBytes = stream.readBytes()
            val sha256 = MessageDigest.getInstance("SHA-256").digest(dataBytes).joinToString("") { "%02x".format(it) }

            val itemHeader = ItemMetadataHeader(
                logicalPath = path,
                category = category,
                sizeBytes = dataBytes.size.toLong(),
                sha256 = sha256,
                lastModifiedUtc = System.currentTimeMillis()
            )

            // Send ITEM_HEADER
            val headerBytes = json.encodeToString(itemHeader).toByteArray(Charsets.UTF_8)
            ProtocolFraming.writePacket(output, MessageTypes.ITEM_HEADER, headerBytes, processedItems)

            // Send CHUNK_DATA (64 KB chunks)
            val chunkSize = 65536
            var offset = 0
            var seq = 0L
            while (offset < dataBytes.size) {
                val end = minOf(offset + chunkSize, dataBytes.size)
                val chunk = dataBytes.copyOfRange(offset, end)
                ProtocolFraming.writePacket(output, MessageTypes.CHUNK_DATA, chunk, seq)

                // Read CHUNK_ACK
                val (ackHeader, _) = ProtocolFraming.readPacket(input)
                if (ackHeader.messageType != MessageTypes.CHUNK_ACK) {
                    throw IllegalStateException("Expected CHUNK_ACK, received ${ackHeader.messageType}")
                }

                offset = end
                seq++
                processedBytes += chunk.size
            }

            // Send ITEM_END
            ProtocolFraming.writePacket(output, MessageTypes.ITEM_END, ByteArray(0), processedItems)

            processedItems++
            TransferForegroundService.updateProgress(
                itemName = path,
                category = category,
                processedBytes = processedBytes,
                totalBytes = processedBytes,
                processedItems = processedItems,
                totalItems = totalItems
            )
        }

        // Send SESSION_COMPLETE
        val summary = SessionSummary(totalItems, processedBytes, processedItems, 0)
        val summaryBytes = json.encodeToString(summary).toByteArray(Charsets.UTF_8)
        ProtocolFraming.writePacket(output, MessageTypes.SESSION_COMPLETE, summaryBytes, 0)
    }

    fun buildCapabilityReport(): DeviceCapabilityReport {
        val permissions = mutableMapOf<String, Boolean>()
        val checkPerm = { perm: String ->
            ContextCompat.checkSelfPermission(context, perm) == android.content.pm.PackageManager.PERMISSION_GRANTED
        }

        permissions["READ_MEDIA_IMAGES"] = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            checkPerm(android.Manifest.permission.READ_MEDIA_IMAGES)
        } else {
            checkPerm(android.Manifest.permission.READ_EXTERNAL_STORAGE)
        }

        permissions["READ_MEDIA_VIDEO"] = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            checkPerm(android.Manifest.permission.READ_MEDIA_VIDEO)
        } else {
            checkPerm(android.Manifest.permission.READ_EXTERNAL_STORAGE)
        }

        permissions["READ_MEDIA_AUDIO"] = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            checkPerm(android.Manifest.permission.READ_MEDIA_AUDIO)
        } else {
            checkPerm(android.Manifest.permission.READ_EXTERNAL_STORAGE)
        }

        permissions["READ_CONTACTS"] = checkPerm(android.Manifest.permission.READ_CONTACTS)
        permissions["READ_CALENDAR"] = checkPerm(android.Manifest.permission.READ_CALENDAR)

        val supported = mutableListOf("DeviceIdentity")
        if (permissions["READ_MEDIA_IMAGES"] == true) supported.add("Photos")
        if (permissions["READ_MEDIA_VIDEO"] == true) supported.add("Videos")
        if (permissions["READ_MEDIA_AUDIO"] == true) supported.add("Audio")
        if (permissions["READ_CONTACTS"] == true) supported.add("Contacts")
        if (permissions["READ_CALENDAR"] == true) supported.add("Calendar")
        supported.add("AppInventory")

        val restricted = mutableMapOf<String, String>()
        restricted["AppInternalData"] = "Prohibited without root access by Android SElinux sandbox rules."
        restricted["SmsMessages"] = "Requires default SMS handler role under Google Play policy."
        restricted["CallLogs"] = "Requires default phone/dialer role under Google Play policy."

        return DeviceCapabilityReport(
            deviceId = Build.SERIAL.ifBlank { UUID.randomUUID().toString() },
            manufacturer = Build.MANUFACTURER,
            model = Build.MODEL,
            product = Build.PRODUCT,
            androidVersion = Build.VERSION.RELEASE,
            apiLevel = Build.VERSION.SDK_INT,
            buildFingerprint = Build.FINGERPRINT,
            isRooted = false,
            grantedPermissions = permissions,
            supportedCategories = supported,
            restrictedCategories = restricted
        )
    }
}
