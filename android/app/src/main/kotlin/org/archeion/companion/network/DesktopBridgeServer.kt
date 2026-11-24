package org.archeion.companion.network

import android.content.Context
import android.os.Build
import android.util.Log
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
    companion object {
        private const val TAG = "ArcheionBridge"
    }

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
    private val nsdManager = ArcheionNsdManager(context)

    fun start() {
        if (serverJob != null && serverJob?.isActive == true) return

        serverJob = scope.launch {
            try {
                serverSocket = ServerSocket(port)
                _bridgeState.value = BridgeState.Listening
                Log.i(TAG, "ARCHEION Bridge Server started listening on port $port")
                nsdManager.registerService(port)

                while (isActive) {
                    val clientSocket = serverSocket?.accept() ?: break
                    _bridgeState.value = BridgeState.Connected
                    Log.i(TAG, "Accepted connection from ${clientSocket.inetAddress.hostAddress}")
                    handleClientConnection(clientSocket)
                    _bridgeState.value = BridgeState.Listening
                }
            } catch (e: Exception) {
                Log.e(TAG, "Server socket exception: ${e.message}", e)
                _bridgeState.value = BridgeState.Stopped
            }
        }
    }

    fun stop() {
        try {
            nsdManager.unregisterService()
            serverSocket?.close()
            serverJob?.cancel()
            Log.i(TAG, "ARCHEION Bridge Server stopped")
        } catch (e: Exception) {
            Log.w(TAG, "Error while stopping bridge: ${e.message}")
        }
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
                    Log.d(TAG, "Received packet type: 0x${header.messageType.toString(16)} (seq=${header.sequenceNumber}, len=${header.payloadLength})")

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
                            Log.i(TAG, "Handshake accepted. Issued session token: $sessionToken")
                        }

                        MessageTypes.CAPABILITY_REPORT -> {
                            val report = buildCapabilityReport()
                            val jsonBytes = json.encodeToString(report).toByteArray(Charsets.UTF_8)
                            ProtocolFraming.writePacket(output, MessageTypes.CAPABILITY_REPORT, jsonBytes, header.sequenceNumber)
                            Log.i(TAG, "Dispatched live Capability Report (${jsonBytes.size} bytes)")
                        }

                        MessageTypes.START_BACKUP -> {
                            val payloadStr = String(payload, Charsets.UTF_8)
                            val request = json.decodeFromString<StartBackupRequest>(payloadStr)
                            _bridgeState.value = BridgeState.Transferring
                            Log.i(TAG, "Starting backup transfer for categories: ${request.requestedCategories}")
                            executeBackupStreaming(output, input, request.requestedCategories)
                            _bridgeState.value = BridgeState.Connected
                        }

                        MessageTypes.SESSION_COMPLETE -> {
                            Log.i(TAG, "Session completion acknowledged by client.")
                            TransferForegroundService.completeTransfer()
                            break
                        }

                        else -> {
                            Log.w(TAG, "Unknown message type received: ${header.messageType}")
                        }
                    }
                }
            } catch (e: Exception) {
                if (e is IllegalStateException && e.message?.contains("End of stream") == true) {
                    Log.i(TAG, "Bridge client disconnected normally.")
                } else {
                    Log.e(TAG, "Error during bridge session handling: ${e.message}", e)
                    try {
                        val errJson = """{"error":"${e.message?.replace("\"", "\\\"")}"}"""
                        ProtocolFraming.writePacket(output, MessageTypes.ERROR, errJson.toByteArray(Charsets.UTF_8))
                    } catch (_: Exception) {}
                }
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

        Log.i(TAG, "Total backup items to stream: $totalItems")

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
                    throw IllegalStateException("Expected CHUNK_ACK, received 0x${ackHeader.messageType.toString(16)}")
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
        TransferForegroundService.recordCompletion(summary)
        Log.i(TAG, "Backup streaming completed successfully: $processedItems items, $processedBytes bytes")
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

        val supported = mutableListOf(
            "DeviceIdentity",
            "Photos",
            "Videos",
            "Audio",
            "Contacts",
            "Calendar",
            "AppInventory"
        )

        val restricted = mapOf(
            "AppInternalData" to "Prohibited without root access by Android SElinux sandbox rules.",
            "SmsMessages" to "Requires default SMS handler role under Google Play policy.",
            "CallLogs" to "Requires default phone/dialer role under Google Play policy."
        )

        return DeviceCapabilityReport(
            deviceId = "unknown",
            manufacturer = Build.MANUFACTURER,
            model = Build.MODEL,
            product = Build.PRODUCT,
            androidVersion = Build.VERSION.RELEASE,
            apiLevel = Build.VERSION.SDK_INT,
            buildFingerprint = Build.FINGERPRINT,
            isRooted = checkIsRooted(),
            grantedPermissions = permissions,
            supportedCategories = supported,
            restrictedCategories = restricted
        )
    }

    private fun checkIsRooted(): Boolean {
        val paths = arrayOf(
            "/system/app/Superuser.apk",
            "/sbin/su",
            "/system/bin/su",
            "/system/xbin/su",
            "/data/local/xbin/su",
            "/data/local/bin/su",
            "/system/sd/xbin/su",
            "/system/bin/failsafe/su",
            "/data/local/su"
        )
        for (path in paths) {
            if (java.io.File(path).exists()) return true
        }
        return false
    }
}
