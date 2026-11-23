using System.Text.Json.Serialization;

namespace Archeion.Contracts;

public static class ProtocolConstants
{
    public const string CurrentProtocolVersion = "1.0.0";
    public const int DefaultCompanionPort = 42842;
    public const int MaxChunkSize = 1024 * 1024; // 1 MB chunk
}

public sealed record MessageEnvelope
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; init; } = ProtocolConstants.CurrentProtocolVersion;

    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; init; }

    [JsonPropertyName("messageId")]
    public Guid MessageId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("correlationId")]
    public Guid? CorrelationId { get; init; }

    [JsonPropertyName("messageType")]
    public string MessageType { get; init; } = string.Empty;

    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("payloadJson")]
    public string PayloadJson { get; init; } = "{}";
}

public sealed record HelloPayload(
    string ClientName,
    string ClientVersion,
    string Platform,
    string[] SupportedFeatures
);

public sealed record DeviceInfoPayload(
    string DeviceId,
    string Manufacturer,
    string Model,
    string AndroidVersion,
    int ApiLevel,
    string BuildFingerprint
);

public sealed record CapabilityReportPayload(
    string DeviceId,
    List<CapabilityItemDto> Capabilities
);

public sealed record CapabilityItemDto(
    BackupCategory Category,
    CapabilityStatus Status,
    string Reason,
    string RequiredPermission,
    long EstimatedItemCount,
    long EstimatedSizeBytes
);

public sealed record DataChunkPayload(
    Guid TransferId,
    string ObjectId,
    long ChunkIndex,
    long TotalChunks,
    long ByteOffset,
    int Length,
    string ChecksumSha256
);

public sealed record ChunkAckPayload(
    Guid TransferId,
    long ChunkIndex,
    bool Success,
    string? ErrorMessage
);

public static class ArcheionBinaryFraming
{
    public static readonly byte[] MagicBytes = [0x41, 0x52, 0x43, 0x48]; // 'ARCH'
    public const int HeaderSize = 16;

    public static byte[] CreatePacket(ushort messageType, ReadOnlySpan<byte> payload, uint sequenceNumber = 0, ushort version = 1)
    {
        var packet = new byte[HeaderSize + payload.Length];
        MagicBytes.CopyTo(packet, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4, 2), version);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(6, 2), messageType);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(8, 4), payload.Length);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(12, 4), sequenceNumber);
        payload.CopyTo(packet.AsSpan(HeaderSize));
        return packet;
    }

    public static (ushort Version, ushort MessageType, int PayloadLength, uint SequenceNumber, byte[] Payload) ParsePacket(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize) throw new ArgumentException("Data too short for ARCHEION header.");
        if (!data[..4].SequenceEqual(MagicBytes)) throw new InvalidOperationException("Invalid magic bytes.");
        var version = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(data.Slice(4, 2));
        var msgType = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(data.Slice(6, 2));
        var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data.Slice(8, 4));
        var seq = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(data.Slice(12, 4));
        if (data.Length < HeaderSize + length) throw new ArgumentException("Incomplete packet payload.");
        var payload = data.Slice(HeaderSize, length).ToArray();
        return (version, msgType, length, seq, payload);
    }
}
