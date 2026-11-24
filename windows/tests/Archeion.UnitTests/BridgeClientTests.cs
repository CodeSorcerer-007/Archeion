using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Archeion.Adb;
using Archeion.Contracts;
using Xunit;

namespace Archeion.UnitTests;

public class BridgeClientTests
{
    [Fact]
    public async Task BridgeClient_HandshakeAndCapabilityQuery_SucceedsAgainstMockServer()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();

            // 1. Read Handshake Request
            var (ver, type, len, seq, payload) = await ReadPacketAsync(stream);
            Assert.Equal(ArcheionBridgeClient.MsgHandshakeRequest, type);

            // Send Handshake Response
            var hsResponse = JsonSerializer.Serialize(new
            {
                serverName = "MockCompanion",
                serverVersion = "1.0.0",
                accepted = true,
                sessionToken = "test-token-xyz-123",
                deviceFingerprint = "mock/device/test:16/key"
            });
            await SendPacketAsync(stream, ArcheionBridgeClient.MsgHandshakeResponse, Encoding.UTF8.GetBytes(hsResponse), seq);

            // 2. Read Capability Report Request
            var (_, capType, _, capSeq, _) = await ReadPacketAsync(stream);
            Assert.Equal(ArcheionBridgeClient.MsgCapabilityReport, capType);

            // Send Capability Report
            var capResponse = JsonSerializer.Serialize(new
            {
                deviceId = "mock_device_1",
                manufacturer = "MockOEM",
                model = "MockPhone",
                product = "MockPhone",
                androidVersion = "16",
                apiLevel = 36,
                buildFingerprint = "mock/fingerprint",
                isRooted = false,
                grantedPermissions = new Dictionary<string, bool> { ["READ_CONTACTS"] = true },
                supportedCategories = new List<string> { "Contacts", "AppInventory" },
                restrictedCategories = new Dictionary<string, string>()
            });
            await SendPacketAsync(stream, ArcheionBridgeClient.MsgCapabilityReport, Encoding.UTF8.GetBytes(capResponse), capSeq);
        });

        using var client = new ArcheionBridgeClient();
        var connected = await client.ConnectAsync("127.0.0.1", port);
        Assert.True(connected);

        var hsResult = await client.HandshakeAsync("TestDesktop", "1.0.0");
        Assert.True(hsResult.Accepted);
        Assert.Equal("test-token-xyz-123", hsResult.SessionToken);
        Assert.Equal("MockCompanion", hsResult.ServerName);

        var caps = await client.QueryCapabilitiesAsync();
        Assert.Equal("MockPhone", caps.Model);
        Assert.Equal(36, caps.ApiLevel);
        Assert.False(caps.IsRooted);

        await serverTask;
        listener.Stop();
    }

    [Fact]
    public async Task BridgeClient_StreamBackup_ReceivesChunksAndVerifiesSha256()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var tempDir = Path.Combine(Path.GetTempPath(), $"archeion_test_{Guid.NewGuid()}");

        var testData = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog repeatedly to test multi-chunk preservation integrity.");
        var testSha256 = Convert.ToHexStringLower(SHA256.HashData(testData));

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();

            // Handshake
            var (_, _, _, seq1, _) = await ReadPacketAsync(stream);
            var hsResponse = JsonSerializer.Serialize(new { accepted = true, sessionToken = "valid-token" });
            await SendPacketAsync(stream, ArcheionBridgeClient.MsgHandshakeResponse, Encoding.UTF8.GetBytes(hsResponse), seq1);

            // Start Backup
            var (_, bType, _, _, bPayload) = await ReadPacketAsync(stream);
            Assert.Equal(ArcheionBridgeClient.MsgStartBackup, bType);

            // Stream item 1 header
            var itemHeader = JsonSerializer.Serialize(new
            {
                logicalPath = "notes/test.txt",
                category = "Documents",
                sizeBytes = testData.Length,
                sha256 = testSha256,
                lastModifiedUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            await SendPacketAsync(stream, ArcheionBridgeClient.MsgItemHeader, Encoding.UTF8.GetBytes(itemHeader), 10);

            // Send chunk 1
            var chunk1 = testData[..30];
            await SendPacketAsync(stream, ArcheionBridgeClient.MsgChunkData, chunk1, 0);

            // Receive Chunk ACK 0
            var (_, ackType0, _, ackSeq0, ackPayload0) = await ReadPacketAsync(stream);
            Assert.Equal(ArcheionBridgeClient.MsgChunkAck, ackType0);

            // Send chunk 2
            var chunk2 = testData[30..];
            await SendPacketAsync(stream, ArcheionBridgeClient.MsgChunkData, chunk2, 1);

            // Receive Chunk ACK 1
            var (_, ackType1, _, ackSeq1, ackPayload1) = await ReadPacketAsync(stream);
            Assert.Equal(ArcheionBridgeClient.MsgChunkAck, ackType1);

            // Send Item End
            await SendPacketAsync(stream, ArcheionBridgeClient.MsgItemEnd, [], 11);

            // Send Session Complete
            var completeSummary = JsonSerializer.Serialize(new
            {
                totalItems = 1L,
                totalBytes = (long)testData.Length,
                successfulItems = 1L,
                failedItems = 0L
            });
            await SendPacketAsync(stream, ArcheionBridgeClient.MsgSessionComplete, Encoding.UTF8.GetBytes(completeSummary), 12);
        });

        using var client = new ArcheionBridgeClient();
        await client.ConnectAsync("127.0.0.1", port);
        await client.HandshakeAsync();

        var updates = new List<TransferProgressUpdate>();
        var progress = new Progress<TransferProgressUpdate>(u => updates.Add(u));

        var backupResult = await client.StreamBackupAsync(["Documents"], tempDir, progress);

        Assert.True(backupResult.Success);
        Assert.Equal(1, backupResult.TotalItems);
        Assert.Equal(testData.Length, backupResult.TotalBytes);
        Assert.Single(backupResult.SavedFiles);

        // Verify file written to disk
        var writtenFile = backupResult.SavedFiles[0];
        Assert.True(File.Exists(writtenFile));
        var contentOnDisk = await File.ReadAllBytesAsync(writtenFile);
        Assert.Equal(testData, contentOnDisk);

        await serverTask;
        listener.Stop();
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }

    private static async Task<(ushort Version, ushort MessageType, int PayloadLength, uint SequenceNumber, byte[] Payload)> ReadPacketAsync(NetworkStream stream)
    {
        var hdr = new byte[ArcheionBinaryFraming.HeaderSize];
        await stream.ReadExactlyAsync(hdr);

        var version = BinaryPrimitives.ReadUInt16BigEndian(hdr.AsSpan(4, 2));
        var msgType = BinaryPrimitives.ReadUInt16BigEndian(hdr.AsSpan(6, 2));
        var length = BinaryPrimitives.ReadInt32BigEndian(hdr.AsSpan(8, 4));
        var seq = BinaryPrimitives.ReadUInt32BigEndian(hdr.AsSpan(12, 4));

        var payload = new byte[length];
        if (length > 0)
        {
            await stream.ReadExactlyAsync(payload);
        }

        return (version, msgType, length, seq, payload);
    }

    private static async Task SendPacketAsync(NetworkStream stream, ushort type, byte[] payload, uint seq)
    {
        var pkt = ArcheionBinaryFraming.CreatePacket(type, payload, seq);
        await stream.WriteAsync(pkt);
        await stream.FlushAsync();
    }
}
