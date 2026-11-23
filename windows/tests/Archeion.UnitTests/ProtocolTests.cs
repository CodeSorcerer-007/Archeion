using System.Text;
using System.Text.Json;
using Archeion.Contracts;
using Xunit;

namespace Archeion.UnitTests;

public class ProtocolTests
{
    [Fact]
    public void BinaryFraming_CreatesValid16ByteHeaderPacket()
    {
        var payload = Encoding.UTF8.GetBytes("HELLO");
        ushort msgType = 0x0010; // CAPABILITY_REPORT
        uint seq = 42;

        var packet = ArcheionBinaryFraming.CreatePacket(msgType, payload, seq);

        // Assert length
        Assert.Equal(16 + payload.Length, packet.Length);

        // Check magic
        Assert.Equal((byte)'A', packet[0]);
        Assert.Equal((byte)'R', packet[1]);
        Assert.Equal((byte)'C', packet[2]);
        Assert.Equal((byte)'H', packet[3]);

        // Parse back
        var (version, parsedType, length, parsedSeq, parsedPayload) = ArcheionBinaryFraming.ParsePacket(packet);

        Assert.Equal(1, version);
        Assert.Equal(msgType, parsedType);
        Assert.Equal(payload.Length, length);
        Assert.Equal(seq, parsedSeq);
        Assert.Equal("HELLO", Encoding.UTF8.GetString(parsedPayload));
    }

    [Fact]
    public void BinaryFraming_RejectsInvalidMagicBytes()
    {
        var packet = new byte[20];
        packet[0] = 0x58; // 'X'
        packet[1] = 0x58;

        Assert.Throws<InvalidOperationException>(() => ArcheionBinaryFraming.ParsePacket(packet));
    }

    [Fact]
    public void BinaryFraming_RejectsShortData()
    {
        var packet = new byte[10];
        Assert.Throws<ArgumentException>(() => ArcheionBinaryFraming.ParsePacket(packet));
    }

    [Fact]
    public void BinaryFraming_RejectsTruncatedPayload()
    {
        var payload = Encoding.UTF8.GetBytes("LONG_PAYLOAD_CONTENT");
        var packet = ArcheionBinaryFraming.CreatePacket(0x0001, payload, 1);

        // Truncate packet by 5 bytes
        var truncated = packet.AsSpan(0, packet.Length - 5).ToArray();
        Assert.Throws<ArgumentException>(() => ArcheionBinaryFraming.ParsePacket(truncated));
    }

    [Fact]
    public void MessageEnvelope_SerializesAndDeserializesRoundTrip()
    {
        var envelope = new MessageEnvelope
        {
            SessionId = Guid.NewGuid(),
            MessageType = "DeviceInfo",
            PayloadJson = JsonSerializer.Serialize(new DeviceInfoPayload(
                DeviceId: "pixel8_test_123",
                Manufacturer: "Google",
                Model: "Pixel 8",
                AndroidVersion: "15",
                ApiLevel: 35,
                BuildFingerprint: "google/shiba/shiba:15/AP4A/12345:user/release-keys"
            ))
        };

        var json = JsonSerializer.Serialize(envelope);
        var restored = JsonSerializer.Deserialize<MessageEnvelope>(json);

        Assert.NotNull(restored);
        Assert.Equal(envelope.SessionId, restored.SessionId);
        Assert.Equal("DeviceInfo", restored.MessageType);
        Assert.Contains("Pixel 8", restored.PayloadJson);
    }
}
