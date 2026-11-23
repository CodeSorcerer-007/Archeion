package org.archeion.companion.protocol

import java.nio.ByteBuffer
import java.nio.ByteOrder

/**
 * 16-byte fixed binary header for ARCHEION streaming framing:
 * [0..3]   Magic: 0x41 0x52 0x43 0x48 ('ARCH')
 * [4..5]   Protocol Version (UInt16): 1
 * [6..7]   Message Type (UInt16)
 * [8..11]  Payload Length (UInt32)
 * [12..15] Sequence Number (UInt32)
 */
data class ArcheionHeader(
    val version: Int = 1,
    val messageType: Int,
    val payloadLength: Int,
    val sequenceNumber: Long
) {
    companion object {
        val MAGIC_BYTES = byteArrayOf(0x41.toByte(), 0x52.toByte(), 0x43.toByte(), 0x48.toByte())
        const val HEADER_SIZE = 16

        fun fromBytes(bytes: ByteArray): ArcheionHeader {
            require(bytes.size >= HEADER_SIZE) { "Header requires 16 bytes" }
            val buf = ByteBuffer.wrap(bytes).order(ByteOrder.BIG_ENDIAN)
            for (m in MAGIC_BYTES) {
                if (buf.get() != m) throw IllegalArgumentException("Invalid ARCHEION magic header")
            }
            val version = buf.short.toInt() and 0xFFFF
            val msgType = buf.short.toInt() and 0xFFFF
            val length = buf.int
            val seq = buf.int.toLong() and 0xFFFFFFFFL

            return ArcheionHeader(version, msgType, length, seq)
        }
    }

    fun toBytes(): ByteArray {
        val buf = ByteBuffer.allocate(HEADER_SIZE).order(ByteOrder.BIG_ENDIAN)
        buf.put(MAGIC_BYTES)
        buf.putShort(version.toShort())
        buf.putShort(messageType.toShort())
        buf.putInt(payloadLength)
        buf.putInt(sequenceNumber.toInt())
        return buf.array()
    }
}

object MessageTypes {
    const val HANDSHAKE_REQUEST = 0x0001
    const val HANDSHAKE_RESPONSE = 0x0002
    const val CAPABILITY_REPORT = 0x0010
    const val START_BACKUP = 0x0020
    const val START_BACKUP_ACK = 0x0021
    const val ITEM_HEADER = 0x0030
    const val CHUNK_DATA = 0x0031
    const val CHUNK_ACK = 0x0032
    const val ITEM_END = 0x0033
    const val SESSION_COMPLETE = 0x0040
    const val ERROR = 0x00FF
}
