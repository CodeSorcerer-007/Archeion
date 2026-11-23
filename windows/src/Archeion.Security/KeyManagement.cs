using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Archeion.Domain.ValueObjects;

namespace Archeion.Security;

public interface IKeyDerivationService
{
    byte[] DeriveKey(string password, byte[] salt, int iterations = 600000);
    byte[] GenerateSalt(int sizeBytes = 32);
}

public sealed class Pbkdf2KeyDerivationService : IKeyDerivationService
{
    public const int DefaultIterations = 600000;
    public const int KeyLengthBytes = 32;

    public byte[] DeriveKey(string password, byte[] salt, int iterations = DefaultIterations)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }
        ArgumentNullException.ThrowIfNull(salt);
        if (salt.Length < 16)
        {
            throw new ArgumentException("Salt must be at least 16 bytes.", nameof(salt));
        }

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        return Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, iterations, HashAlgorithmName.SHA256, KeyLengthBytes);
    }

    public byte[] GenerateSalt(int sizeBytes = 32)
    {
        return RandomNumberGenerator.GetBytes(sizeBytes);
    }
}

public sealed record KeyEnvelopeData
{
    [JsonPropertyName("formatVersion")]
    public string FormatVersion { get; init; } = "1.0.0";

    [JsonPropertyName("kdf")]
    public string Kdf { get; init; } = "PBKDF2-SHA256";

    [JsonPropertyName("iterations")]
    public int Iterations { get; init; } = Pbkdf2KeyDerivationService.DefaultIterations;

    [JsonPropertyName("saltHex")]
    public string SaltHex { get; init; } = string.Empty;

    [JsonPropertyName("passwordWrappedKeyHex")]
    public string PasswordWrappedKeyHex { get; init; } = string.Empty;

    [JsonPropertyName("recoveryWrappedKeyHex")]
    public string RecoveryWrappedKeyHex { get; init; } = string.Empty;

    [JsonPropertyName("recoveryKeyHint")]
    public string RecoveryKeyHint { get; init; } = string.Empty;
}

public sealed class KeyEnvelopeManager
{
    private readonly IEncryptionService _encryptionService;
    private readonly IKeyDerivationService _kdfService;

    public KeyEnvelopeManager(IEncryptionService encryptionService, IKeyDerivationService kdfService)
    {
        _encryptionService = encryptionService;
        _kdfService = kdfService;
    }

    public (KeyEnvelopeData Envelope, byte[] MasterKey, RecoveryKey RecoveryKey) CreateEnvelope(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters long.", nameof(password));
        }

        var masterKey = RandomNumberGenerator.GetBytes(32);
        var salt = _kdfService.GenerateSalt(32);
        var passwordDerivedKey = _kdfService.DeriveKey(password, salt);
        var recoveryKey = RecoveryKey.Generate();

        var passwordWrapped = _encryptionService.Encrypt(masterKey, passwordDerivedKey, Encoding.UTF8.GetBytes("ArcheionPasswordWrap"));
        var recoveryWrapped = _encryptionService.Encrypt(masterKey, recoveryKey.ToBytes(), Encoding.UTF8.GetBytes("ArcheionRecoveryWrap"));

        var envelope = new KeyEnvelopeData
        {
            SaltHex = Convert.ToHexStringLower(salt),
            PasswordWrappedKeyHex = Convert.ToHexStringLower(passwordWrapped),
            RecoveryWrappedKeyHex = Convert.ToHexStringLower(recoveryWrapped),
            RecoveryKeyHint = recoveryKey.ToString()[..9] + "..." // first 8 chars hint
        };

        return (envelope, masterKey, recoveryKey);
    }

    public byte[] UnlockWithPassword(KeyEnvelopeData envelope, string password)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var salt = Convert.FromHexString(envelope.SaltHex);
        var passwordDerivedKey = _kdfService.DeriveKey(password, salt, envelope.Iterations);
        var wrappedKey = Convert.FromHexString(envelope.PasswordWrappedKeyHex);

        return _encryptionService.Decrypt(wrappedKey, passwordDerivedKey, Encoding.UTF8.GetBytes("ArcheionPasswordWrap"));
    }

    public byte[] UnlockWithRecoveryKey(KeyEnvelopeData envelope, RecoveryKey recoveryKey)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(recoveryKey);
        var wrappedKey = Convert.FromHexString(envelope.RecoveryWrappedKeyHex);

        return _encryptionService.Decrypt(wrappedKey, recoveryKey.ToBytes(), Encoding.UTF8.GetBytes("ArcheionRecoveryWrap"));
    }

    public static string SerializeEnvelope(KeyEnvelopeData envelope) =>
        JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true });

    public static KeyEnvelopeData DeserializeEnvelope(string json) =>
        JsonSerializer.Deserialize<KeyEnvelopeData>(json) ?? throw new InvalidOperationException("Failed to deserialize key envelope.");
}
