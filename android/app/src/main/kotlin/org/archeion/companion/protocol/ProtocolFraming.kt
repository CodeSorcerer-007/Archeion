package org.archeion.companion.protocol

import java.io.InputStream
import java.io.OutputStream

class ProtocolFraming {

    companion object {
        fun readPacket(inputStream: InputStream): Pair<ArcheionHeader, ByteArray> {
            val headerBytes = ByteArray(ArcheionHeader.HEADER_SIZE)
            var read = 0
            while (read < ArcheionHeader.HEADER_SIZE) {
                val r = inputStream.read(headerBytes, read, ArcheionHeader.HEADER_SIZE - read)
                if (r < 0) throw IllegalStateException("End of stream while reading packet header")
                read += r
            }

            val header = ArcheionHeader.fromBytes(headerBytes)
            val payload = ByteArray(header.payloadLength)
            var payloadRead = 0
            while (payloadRead < header.payloadLength) {
                val r = inputStream.read(payload, payloadRead, header.payloadLength - payloadRead)
                if (r < 0) throw IllegalStateException("End of stream while reading packet payload")
                payloadRead += r
            }

            return Pair(header, payload)
        }

        fun writePacket(
            outputStream: OutputStream,
            messageType: Int,
            payload: ByteArray,
            sequenceNumber: Long = 0
        ) {
            val header = ArcheionHeader(
                messageType = messageType,
                payloadLength = payload.size,
                sequenceNumber = sequenceNumber
            )
            outputStream.write(header.toBytes())
            if (payload.isNotEmpty()) {
                outputStream.write(payload)
            }
            outputStream.flush()
        }
    }
}
