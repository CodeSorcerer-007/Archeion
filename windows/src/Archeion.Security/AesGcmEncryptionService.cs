using System.Security.Cryptography;

namespace Archeion.Security;

public interface IEncryptionService
{
    byte[] Encrypt(byte[] plaintext, byte[] key, byte[]? associatedData = null);
    byte[] Decrypt(byte[] encryptedPayload, byte[] key, byte[]? associatedData = null);
    Task EncryptStreamAsync(Stream inputStream, Stream outputStream, byte[] key, byte[]? associatedData = null, CancellationToken cancellationToken = default);
    Task DecryptStreamAsync(Stream encryptedStream, Stream outputStream, byte[] key, byte[]? associatedData = null, CancellationToken cancellationToken = default);
}

public sealed class AesGcmEncryptionService : IEncryptionService
{
    public const int NonceSizeBytes = 12; // 96 bits standard GCM IV
    public const int TagSizeBytes = 16;   // 128 bits standard GCM tag
    public const int KeySizeBytes = 32;   // 256 bits AES key

    public byte[] Encrypt(byte[] plaintext, byte[] key, byte[]? associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ValidateKey(key);

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        // Format: [12 bytes Nonce] [N bytes Ciphertext] [16 bytes Tag]
        var result = new byte[NonceSizeBytes + ciphertext.Length + TagSizeBytes];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSizeBytes, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSizeBytes + ciphertext.Length, TagSizeBytes);

        return result;
    }

    public byte[] Decrypt(byte[] encryptedPayload, byte[] key, byte[]? associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);
        ValidateKey(key);

        if (encryptedPayload.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Encrypted payload is too short to contain a valid nonce and authentication tag.");
        }

        var nonce = new byte[NonceSizeBytes];
        Buffer.BlockCopy(encryptedPayload, 0, nonce, 0, NonceSizeBytes);

        var ciphertextLength = encryptedPayload.Length - NonceSizeBytes - TagSizeBytes;
        var ciphertext = new byte[ciphertextLength];
        Buffer.BlockCopy(encryptedPayload, NonceSizeBytes, ciphertext, 0, ciphertextLength);

        var tag = new byte[TagSizeBytes];
        Buffer.BlockCopy(encryptedPayload, NonceSizeBytes + ciphertextLength, tag, 0, TagSizeBytes);

        var plaintext = new byte[ciphertextLength];
        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

        return plaintext;
    }

    public async Task EncryptStreamAsync(Stream inputStream, Stream outputStream, byte[] key, byte[]? associatedData = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ValidateKey(key);

        using var ms = new MemoryStream();
        await inputStream.CopyToAsync(ms, cancellationToken);
        var encrypted = Encrypt(ms.ToArray(), key, associatedData);
        await outputStream.WriteAsync(encrypted, cancellationToken);
    }

    public async Task DecryptStreamAsync(Stream encryptedStream, Stream outputStream, byte[] key, byte[]? associatedData = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encryptedStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ValidateKey(key);

        using var ms = new MemoryStream();
        await encryptedStream.CopyToAsync(ms, cancellationToken);
        var decrypted = Decrypt(ms.ToArray(), key, associatedData);
        await outputStream.WriteAsync(decrypted, cancellationToken);
    }

    private static void ValidateKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != KeySizeBytes)
        {
            throw new ArgumentException($"Invalid AES-256 key size: {key.Length} bytes. Expected {KeySizeBytes} bytes (256 bits).", nameof(key));
        }
    }
}
