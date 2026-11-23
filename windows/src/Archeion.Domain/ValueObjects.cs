using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Archeion.Domain.ValueObjects;

public readonly partial record struct ContentHash
{
    private static readonly Regex HexRegex = CreateHexRegex();

    public string Value { get; }

    public ContentHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || !HexRegex.IsMatch(value))
        {
            throw new ArgumentException($"Invalid SHA-256 hash: '{value}'. Expected 64 hexadecimal characters.", nameof(value));
        }

        Value = value.ToLowerInvariant();
    }

    public static ContentHash Compute(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var hashBytes = SHA256.HashData(data);
        return new ContentHash(Convert.ToHexStringLower(hashBytes));
    }

    public static async Task<ContentHash> ComputeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return new ContentHash(Convert.ToHexStringLower(hashBytes));
    }

    public static ContentHash Empty => new("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");

    public override string ToString() => Value;

    [GeneratedRegex("^[0-9a-fA-F]{64}$")]
    private static partial Regex CreateHexRegex();
}

public readonly record struct ByteSize(long Bytes) : IComparable<ByteSize>
{
    public static ByteSize Zero => new(0);

    public static ByteSize FromBytes(long bytes) => new(Math.Max(0, bytes));
    public static ByteSize FromKiloBytes(double kb) => new((long)(kb * 1024));
    public static ByteSize FromMegaBytes(double mb) => new((long)(mb * 1024 * 1024));
    public static ByteSize FromGigaBytes(double gb) => new((long)(gb * 1024 * 1024 * 1024));

    public int CompareTo(ByteSize other) => Bytes.CompareTo(other.Bytes);

    public override string ToString()
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double val = Bytes;
        int order = 0;
        while (val >= 1024 && order < units.Length - 1)
        {
            order++;
            val /= 1024;
        }

        return $"{val:0.##} {units[order]}";
    }

    public static ByteSize operator +(ByteSize a, ByteSize b) => new(a.Bytes + b.Bytes);
    public static ByteSize operator -(ByteSize a, ByteSize b) => new(Math.Max(0, a.Bytes - b.Bytes));
}

public sealed record StoragePath
{
    public string NormalizedPath { get; }

    public StoragePath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            throw new ArgumentException("Storage path cannot be empty.", nameof(rawPath));
        }

        // Normalize separators to forward slash
        var normalized = rawPath.Replace('\\', '/').Trim('/');

        // Guard against path traversal
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var seg in segments)
        {
            if (seg == ".." || seg == "." || seg.Contains(':') || seg.IndexOfAny(Path.GetInvalidFileNameChars().Where(c => c != '/').ToArray()) >= 0)
            {
                throw new ArgumentException($"Illegal path segment '{seg}' in path '{rawPath}' detected (path traversal or invalid characters).", nameof(rawPath));
            }
        }

        NormalizedPath = string.Join('/', segments);
    }

    public string FileName => Path.GetFileName(NormalizedPath);

    public override string ToString() => NormalizedPath;

    public static implicit operator string(StoragePath p) => p.NormalizedPath;
}

public sealed record RecoveryKey
{
    public string KeyString { get; }

    public RecoveryKey(string keyString)
    {
        var cleaned = CleanKey(keyString);
        if (cleaned.Length != 64)
        {
            throw new ArgumentException("Recovery key must represent 256 bits of hexadecimal entropy (64 hex characters).", nameof(keyString));
        }
        KeyString = FormatKey(cleaned);
    }

    public static RecoveryKey Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return new RecoveryKey(Convert.ToHexStringLower(bytes));
    }

    public byte[] ToBytes()
    {
        var clean = CleanKey(KeyString);
        return Convert.FromHexString(clean);
    }

    public static string CleanKey(string input) =>
        input.Replace("-", "").Replace(" ", "").Trim().ToLowerInvariant();

    private static string FormatKey(string hex64)
    {
        // Format as 8 groups of 8 characters separated by hyphens: xxxxxxxx-xxxxxxxx-...
        return string.Join("-", Enumerable.Range(0, 8).Select(i => hex64.Substring(i * 8, 8)));
    }

    public override string ToString() => KeyString;
}
