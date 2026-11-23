package org.archeion.companion.protocol

import kotlinx.serialization.Serializable

@Serializable
data class HandshakeRequest(
    val clientName: String,
    val clientVersion: String,
    val protocolVersion: String = "1.0.0"
)

@Serializable
data class HandshakeResponse(
    val serverName: String = "Archeion Android Companion",
    val serverVersion: String = "1.0.0",
    val protocolVersion: String = "1.0.0",
    val deviceFingerprint: String,
    val accepted: Boolean,
    val sessionToken: String
)

@Serializable
data class DeviceCapabilityReport(
    val deviceId: String,
    val manufacturer: String,
    val model: String,
    val product: String,
    val androidVersion: String,
    val apiLevel: Int,
    val buildFingerprint: String,
    val isRooted: Boolean = false,
    val grantedPermissions: Map<String, Boolean>,
    val supportedCategories: List<String>,
    val restrictedCategories: Map<String, String>
)

@Serializable
data class StartBackupRequest(
    val sessionToken: String,
    val requestedCategories: List<String>
)

@Serializable
data class ItemMetadataHeader(
    val logicalPath: String,
    val category: String,
    val sizeBytes: Long,
    val sha256: String,
    val lastModifiedUtc: Long
)

@Serializable
data class ChunkAcknowledgment(
    val sequenceNumber: Long,
    val bytesReceived: Long,
    val sha256Chunk: String,
    val acknowledged: Boolean
)

@Serializable
data class SessionSummary(
    val totalItems: Long,
    val totalBytes: Long,
    val successfulItems: Long,
    val failedItems: Long
)
