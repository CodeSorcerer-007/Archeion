using System.Security.Cryptography;
using System.Text;
using Archeion.Domain.ValueObjects;
using Archeion.Security;
using Xunit;

namespace Archeion.SecurityTests;

public class SecurityTests
{
    private readonly AesGcmEncryptionService _encryptionService = new();
    private readonly Pbkdf2KeyDerivationService _kdfService = new();

    [Fact]
    public void Encrypt_Decrypt_RoundTrip_Succeeds()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("Archeion Test Personal Android Backup Payload");
        var aad = Encoding.UTF8.GetBytes("Repo-12345");

        var encrypted = _encryptionService.Encrypt(plaintext, key, aad);
        Assert.NotNull(encrypted);
        Assert.True(encrypted.Length > plaintext.Length);

        var decrypted = _encryptionService.Decrypt(encrypted, key, aad);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsCryptographicException()
    {
        var key1 = RandomNumberGenerator.GetBytes(32);
        var key2 = RandomNumberGenerator.GetBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("Secret Data");

        var encrypted = _encryptionService.Encrypt(plaintext, key1);

        Assert.ThrowsAny<CryptographicException>(() => _encryptionService.Decrypt(encrypted, key2));
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_ThrowsCryptographicException()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("Important Database Payload");
        var encrypted = _encryptionService.Encrypt(plaintext, key);

        // Tamper with one byte in the ciphertext body
        encrypted[15] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => _encryptionService.Decrypt(encrypted, key));
    }

    [Fact]
    public void Decrypt_WithMismatchedAssociatedData_ThrowsCryptographicException()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("Sensitive photo chunk");
        var aad1 = Encoding.UTF8.GetBytes("Object-A");
        var aad2 = Encoding.UTF8.GetBytes("Object-B");

        var encrypted = _encryptionService.Encrypt(plaintext, key, aad1);

        Assert.ThrowsAny<CryptographicException>(() => _encryptionService.Decrypt(encrypted, key, aad2));
    }

    [Fact]
    public void KeyEnvelope_Password_And_RecoveryKey_RoundTrip_Succeeds()
    {
        var manager = new KeyEnvelopeManager(_encryptionService, _kdfService);
        const string password = "StrongMasterPassword2026!";

        var (envelope, masterKey, recoveryKey) = manager.CreateEnvelope(password);

        Assert.NotNull(envelope);
        Assert.Equal(32, masterKey.Length);
        Assert.NotNull(recoveryKey);

        // 1. Unlock with password
        var unlockedWithPassword = manager.UnlockWithPassword(envelope, password);
        Assert.Equal(masterKey, unlockedWithPassword);

        // 2. Unlock with recovery key
        var unlockedWithRecoveryKey = manager.UnlockWithRecoveryKey(envelope, recoveryKey);
        Assert.Equal(masterKey, unlockedWithRecoveryKey);

        // 3. Wrong password fails
        Assert.ThrowsAny<CryptographicException>(() => manager.UnlockWithPassword(envelope, "WrongPassword!"));

        // 4. Wrong recovery key fails
        var fakeRecoveryKey = RecoveryKey.Generate();
        Assert.ThrowsAny<CryptographicException>(() => manager.UnlockWithRecoveryKey(envelope, fakeRecoveryKey));
    }

    [Fact]
    public async Task Stream_Encrypt_Decrypt_RoundTrip_Succeeds()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var testData = RandomNumberGenerator.GetBytes(128 * 1024); // 128 KB
        using var inputStream = new MemoryStream(testData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        await _encryptionService.EncryptStreamAsync(inputStream, encryptedStream, key);
        encryptedStream.Position = 0;

        await _encryptionService.DecryptStreamAsync(encryptedStream, decryptedStream, key);
        Assert.Equal(testData, decryptedStream.ToArray());
    }
}
