using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Archeion.Contracts;

namespace Archeion.Adb;

public sealed record HandshakeResult(
    bool Accepted,
    string SessionToken,
    string ServerName,
    string ServerVersion,
    string DeviceFingerprint
);

public sealed record DeviceCapabilityResult(
    string DeviceId,
    string Manufacturer,
    string Model,
    string Product,
    string AndroidVersion,
    int ApiLevel,
    string BuildFingerprint,
    bool IsRooted,
    Dictionary<string, bool> GrantedPermissions,
    List<string> SupportedCategories,
    Dictionary<string, string> RestrictedCategories
);

public sealed record TransferProgressUpdate(
    string CurrentCategory,
    string CurrentItem,
    long ProcessedBytes,
    long TotalBytes,
    long ProcessedItems
);

public sealed record BackupSessionResult(
    bool Success,
    long TotalItems,
    long TotalBytes,
    long SuccessfulItems,
    long FailedItems,
    List<string> SavedFiles,
    string? ErrorMessage = null
);

public interface IArcheionBridgeClient : IDisposable
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync(string host = "127.0.0.1", int port = 8742, CancellationToken cancellationToken = default);
    Task<HandshakeResult> HandshakeAsync(string clientName = "ArcheionDesktop", string clientVersion = "1.0.0", CancellationToken cancellationToken = default);
    Task<DeviceCapabilityResult> QueryCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<BackupSessionResult> StreamBackupAsync(
        IEnumerable<string> requestedCategories,
        string destinationDirectory,
        IProgress<TransferProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default
    );
    void Disconnect();
}

public class ArcheionBridgeClient : IArcheionBridgeClient
{
    public const ushort MsgHandshakeRequest = 0x0001;
    public const ushort MsgHandshakeResponse = 0x0002;
    public const ushort MsgCapabilityReport = 0x0010;
    public const ushort MsgStartBackup = 0x0020;
    public const ushort MsgStartBackupAck = 0x0021;
    public const ushort MsgItemHeader = 0x0030;
    public const ushort MsgChunkData = 0x0031;
    public const ushort MsgChunkAck = 0x0032;
    public const ushort MsgItemEnd = 0x0033;
    public const ushort MsgSessionComplete = 0x0040;
    public const ushort MsgError = 0x00FF;

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private string? _currentSessionToken;
    private uint _sequenceCounter = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

    public async Task<bool> ConnectAsync(string host = "127.0.0.1", int port = 8742, CancellationToken cancellationToken = default)
    {
        Disconnect();
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port, cancellationToken);
        _stream = _tcpClient.GetStream();
        _sequenceCounter = 1;
        return IsConnected;
    }

    public async Task<HandshakeResult> HandshakeAsync(string clientName = "ArcheionDesktop", string clientVersion = "1.0.0", CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        var requestObj = new
        {
            clientName,
            clientVersion,
            protocolVersion = "1.0.0"
        };
        var reqBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestObj, JsonOptions));

        await SendPacketAsync(MsgHandshakeRequest, reqBytes, _sequenceCounter++, cancellationToken);

        var (type, payload) = await ReadPacketAsync(cancellationToken);
        if (type != MsgHandshakeResponse)
        {
            throw new InvalidOperationException($"Expected HandshakeResponse (0x0002), got 0x{type:X4}");
        }

        var jsonStr = Encoding.UTF8.GetString(payload);
        using var doc = JsonDocument.Parse(jsonStr);
        var root = doc.RootElement;

        var accepted = root.GetProperty("accepted").GetBoolean();
        var sessionToken = root.GetProperty("sessionToken").GetString() ?? string.Empty;
        var serverName = root.TryGetProperty("serverName", out var sn) ? sn.GetString() ?? "" : "";
        var serverVer = root.TryGetProperty("serverVersion", out var sv) ? sv.GetString() ?? "" : "";
        var fingerprint = root.TryGetProperty("deviceFingerprint", out var fp) ? fp.GetString() ?? "" : "";

        _currentSessionToken = sessionToken;
        return new HandshakeResult(accepted, sessionToken, serverName, serverVer, fingerprint);
    }

    public async Task<DeviceCapabilityResult> QueryCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        await SendPacketAsync(MsgCapabilityReport, [], _sequenceCounter++, cancellationToken);

        var (type, payload) = await ReadPacketAsync(cancellationToken);
        if (type != MsgCapabilityReport)
        {
            throw new InvalidOperationException($"Expected CapabilityReport (0x0010), got 0x{type:X4}");
        }

        var jsonStr = Encoding.UTF8.GetString(payload);
        var result = JsonSerializer.Deserialize<DeviceCapabilityResult>(jsonStr, JsonOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize capability report.");
    }

    public async Task<BackupSessionResult> StreamBackupAsync(
        IEnumerable<string> requestedCategories,
        string destinationDirectory,
        IProgress<TransferProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        EnsureConnected();
        if (string.IsNullOrEmpty(_currentSessionToken))
        {
            throw new InvalidOperationException("Must execute HandshakeAsync before starting backup stream.");
        }

        Directory.CreateDirectory(destinationDirectory);

        var requestObj = new
        {
            sessionToken = _currentSessionToken,
            requestedCategories = requestedCategories.ToList()
        };
        var reqBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestObj, JsonOptions));
        await SendPacketAsync(MsgStartBackup, reqBytes, _sequenceCounter++, cancellationToken);

        var savedFiles = new List<string>();
        FileStream? currentFileStream = null;
        IncrementalHash? currentHasher = null;
        string? currentItemExpectedSha = null;
        string currentCategory = "";
        string currentLogicalPath = "";
        long processedBytes = 0;
        long processedItems = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (type, seq, payload) = await ReadPacketWithSeqAsync(cancellationToken);

                if (type == MsgItemHeader)
                {
                    // Close previous if any
                    currentFileStream?.Dispose();
                    currentFileStream = null;
                    currentHasher?.Dispose();
                    currentHasher = null;

                    var jsonStr = Encoding.UTF8.GetString(payload);
                    using var doc = JsonDocument.Parse(jsonStr);
                    var root = doc.RootElement;

                    currentLogicalPath = root.GetProperty("logicalPath").GetString() ?? $"item_{Guid.NewGuid()}";
                    currentCategory = root.GetProperty("category").GetString() ?? "Unknown";
                    currentItemExpectedSha = root.TryGetProperty("sha256", out var sha) ? sha.GetString() : null;

                    var targetPath = Path.Combine(destinationDirectory, currentCategory, currentLogicalPath.Replace('/', Path.DirectorySeparatorChar));
                    var dir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                    currentFileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    currentHasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    savedFiles.Add(targetPath);
                    processedItems++;

                    progress?.Report(new TransferProgressUpdate(currentCategory, currentLogicalPath, processedBytes, 0, processedItems));
                }
                else if (type == MsgChunkData)
                {
                    if (currentFileStream != null && currentHasher != null)
                    {
                        await currentFileStream.WriteAsync(payload, cancellationToken);
                        currentHasher.AppendData(payload);
                        processedBytes += payload.Length;

                        // Acknowledge Chunk
                        var ackObj = new
                        {
                            sequenceNumber = seq,
                            bytesReceived = (long)payload.Length,
                            sha256Chunk = "",
                            acknowledged = true
                        };
                        var ackBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ackObj, JsonOptions));
                        await SendPacketAsync(MsgChunkAck, ackBytes, _sequenceCounter++, cancellationToken);

                        progress?.Report(new TransferProgressUpdate(currentCategory, currentLogicalPath, processedBytes, 0, processedItems));
                    }
                }
                else if (type == MsgItemEnd)
                {
                    if (currentFileStream != null && currentHasher != null)
                    {
                        await currentFileStream.FlushAsync(cancellationToken);
                        var computedSha = Convert.ToHexStringLower(currentHasher.GetHashAndReset());
                        currentFileStream.Dispose();
                        currentFileStream = null;
                        currentHasher.Dispose();
                        currentHasher = null;

                        if (!string.IsNullOrEmpty(currentItemExpectedSha) &&
                            !string.Equals(currentItemExpectedSha, computedSha, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"SHA-256 integrity mismatch for {currentLogicalPath}! Expected: {currentItemExpectedSha}, Computed: {computedSha}");
                        }
                    }
                }
                else if (type == MsgSessionComplete)
                {
                    var jsonStr = Encoding.UTF8.GetString(payload);
                    using var doc = JsonDocument.Parse(jsonStr);
                    var root = doc.RootElement;

                    var totalItems = root.GetProperty("totalItems").GetInt64();
                    var totalBytes = root.GetProperty("totalBytes").GetInt64();
                    var successful = root.GetProperty("successfulItems").GetInt64();
                    var failed = root.GetProperty("failedItems").GetInt64();

                    return new BackupSessionResult(
                        Success: failed == 0,
                        TotalItems: totalItems,
                        TotalBytes: totalBytes,
                        SuccessfulItems: successful,
                        FailedItems: failed,
                        SavedFiles: savedFiles
                    );
                }
                else if (type == MsgError)
                {
                    var err = Encoding.UTF8.GetString(payload);
                    return new BackupSessionResult(
                        Success: false,
                        TotalItems: processedItems,
                        TotalBytes: processedBytes,
                        SuccessfulItems: 0,
                        FailedItems: 1,
                        SavedFiles: savedFiles,
                        ErrorMessage: err
                    );
                }
            }
        }
        finally
        {
            currentFileStream?.Dispose();
            currentHasher?.Dispose();
        }

        return new BackupSessionResult(false, processedItems, processedBytes, 0, 0, savedFiles, "Operation cancelled");
    }

    private async Task SendPacketAsync(ushort messageType, byte[] payload, uint sequenceNumber, CancellationToken ct)
    {
        var packet = ArcheionBinaryFraming.CreatePacket(messageType, payload, sequenceNumber);
        await _stream!.WriteAsync(packet, ct);
        await _stream.FlushAsync(ct);
    }

    private async Task<(ushort MessageType, byte[] Payload)> ReadPacketAsync(CancellationToken ct)
    {
        var (type, _, payload) = await ReadPacketWithSeqAsync(ct);
        return (type, payload);
    }

    private async Task<(ushort MessageType, uint Sequence, byte[] Payload)> ReadPacketWithSeqAsync(CancellationToken ct)
    {
        var headerBytes = new byte[ArcheionBinaryFraming.HeaderSize];
        await _stream!.ReadExactlyAsync(headerBytes, ct);

        if (!headerBytes.AsSpan(0, 4).SequenceEqual(ArcheionBinaryFraming.MagicBytes))
        {
            throw new InvalidOperationException("Invalid magic header received from companion.");
        }

        var version = BinaryPrimitives.ReadUInt16BigEndian(headerBytes.AsSpan(4, 2));
        var msgType = BinaryPrimitives.ReadUInt16BigEndian(headerBytes.AsSpan(6, 2));
        var payloadLength = BinaryPrimitives.ReadInt32BigEndian(headerBytes.AsSpan(8, 4));
        var seq = BinaryPrimitives.ReadUInt32BigEndian(headerBytes.AsSpan(12, 4));

        var payload = new byte[payloadLength];
        if (payloadLength > 0)
        {
            await _stream.ReadExactlyAsync(payload, ct);
        }

        return (msgType, seq, payload);
    }

    private void EnsureConnected()
    {
        if (!IsConnected || _stream == null)
        {
            throw new InvalidOperationException("ArcheionBridgeClient is not connected. Call ConnectAsync first.");
        }
    }

    public void Disconnect()
    {
        try
        {
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
        catch { }
        finally
        {
            _stream = null;
            _tcpClient = null;
            _currentSessionToken = null;
        }
    }

    public void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }
}
