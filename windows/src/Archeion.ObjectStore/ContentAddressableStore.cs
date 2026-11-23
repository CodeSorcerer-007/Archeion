using System.Security.Cryptography;
using Archeion.Contracts;
using Archeion.Domain.ValueObjects;
using Archeion.Security;

namespace Archeion.ObjectStore;

public interface IObjectStore
{
    Task<ContentHash> PutAsync(Stream contentStream, string jobId, byte[]? encryptionKey = null, byte[]? associatedData = null, CancellationToken cancellationToken = default);
    Task<Stream> GetAsync(ContentHash hash, byte[]? encryptionKey = null, byte[]? associatedData = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ContentHash hash);
    Task<bool> VerifyObjectIntegrityAsync(ContentHash hash, byte[]? encryptionKey = null, byte[]? associatedData = null, CancellationToken cancellationToken = default);
    Task<int> CleanupStagingAsync(string jobId);
    Task<List<ContentHash>> ListAllObjectsAsync();
    string GetObjectPhysicalPath(ContentHash hash);
}

public sealed class ContentAddressableStore : IObjectStore
{
    private readonly string _repositoryRoot;
    private readonly string _objectsRoot;
    private readonly string _stagingRoot;
    private readonly IEncryptionService _encryptionService;

    public ContentAddressableStore(string repositoryRoot, IEncryptionService encryptionService)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        _repositoryRoot = repositoryRoot;
        _objectsRoot = Path.Combine(repositoryRoot, "objects");
        _stagingRoot = Path.Combine(repositoryRoot, "staging");
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));

        Directory.CreateDirectory(_objectsRoot);
        Directory.CreateDirectory(_stagingRoot);
    }

    public async Task<ContentHash> PutAsync(
        Stream contentStream,
        string jobId,
        byte[]? encryptionKey = null,
        byte[]? associatedData = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contentStream);
        ArgumentNullException.ThrowIfNull(jobId);

        var jobStagingDir = Path.Combine(_stagingRoot, SanitizePath(jobId));
        Directory.CreateDirectory(jobStagingDir);

        var tempFilePath = Path.Combine(jobStagingDir, $"{Guid.NewGuid():N}.tmp");

        try
        {
            ContentHash plaintextHash;
            // 1. Stage and compute SHA-256
            if (encryptionKey != null)
            {
                // In encrypted mode: calculate hash of plaintext while encrypting to staged file
                using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                using (var plaintextBuffer = new MemoryStream())
                {
                    var buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        sha256.AppendData(buffer, 0, bytesRead);
                        plaintextBuffer.Write(buffer, 0, bytesRead);
                    }

                    var hashBytes = sha256.GetHashAndReset();
                    plaintextHash = new ContentHash(Convert.ToHexStringLower(hashBytes));

                    var effectiveAad = associatedData ?? System.Text.Encoding.UTF8.GetBytes("ArcheionObject:" + plaintextHash.Value);

                    plaintextBuffer.Position = 0;
                    await using var stagingFile = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                    await _encryptionService.EncryptStreamAsync(plaintextBuffer, stagingFile, encryptionKey, effectiveAad, cancellationToken);
                }
            }
            else
            {
                // Unencrypted mode: stream directly to staging file while calculating SHA-256
                await using var stagingFile = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    sha256.AppendData(buffer, 0, bytesRead);
                    await stagingFile.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }

                var hashBytes = sha256.GetHashAndReset();
                plaintextHash = new ContentHash(Convert.ToHexStringLower(hashBytes));
            }

            // 2. Atomic promotion to objects/xx/<hash>
            var finalPath = GetObjectPhysicalPath(plaintextHash);
            var parentDir = Path.GetDirectoryName(finalPath)!;
            Directory.CreateDirectory(parentDir);

            if (!File.Exists(finalPath))
            {
                File.Move(tempFilePath, finalPath, overwrite: false);
            }
            else
            {
                // Object already exists (deduplication) - discard staged temp file safely
                File.Delete(tempFilePath);
            }

            return plaintextHash;
        }
        catch
        {
            // Clean up temporary file on failure
            if (File.Exists(tempFilePath))
            {
                try { File.Delete(tempFilePath); } catch { /* best effort */ }
            }
            throw;
        }
    }

    public async Task<Stream> GetAsync(
        ContentHash hash,
        byte[]? encryptionKey = null,
        byte[]? associatedData = null,
        CancellationToken cancellationToken = default)
    {
        var finalPath = GetObjectPhysicalPath(hash);
        if (!File.Exists(finalPath))
        {
            throw new FileNotFoundException($"Object with hash '{hash}' not found in object store.", finalPath);
        }

        if (encryptionKey != null)
        {
            await using var encryptedStream = new FileStream(finalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            var decryptedMs = new MemoryStream();
            var effectiveAad = associatedData ?? System.Text.Encoding.UTF8.GetBytes("ArcheionObject:" + hash.Value);
            await _encryptionService.DecryptStreamAsync(encryptedStream, decryptedMs, encryptionKey, effectiveAad, cancellationToken);
            decryptedMs.Position = 0;

            // Verify decrypted plaintext hash matches object ID
            var computedHash = await ContentHash.ComputeAsync(decryptedMs, cancellationToken);
            if (computedHash != hash)
            {
                throw new CryptographicException($"Integrity violation: Decrypted object hash '{computedHash}' does not match expected '{hash}'.");
            }

            decryptedMs.Position = 0;
            return decryptedMs;
        }
        else
        {
            var stream = new FileStream(finalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            return stream;
        }
    }

    public Task<bool> ExistsAsync(ContentHash hash)
    {
        var finalPath = GetObjectPhysicalPath(hash);
        return Task.FromResult(File.Exists(finalPath));
    }

    public async Task<bool> VerifyObjectIntegrityAsync(
        ContentHash hash,
        byte[]? encryptionKey = null,
        byte[]? associatedData = null,
        CancellationToken cancellationToken = default)
    {
        var finalPath = GetObjectPhysicalPath(hash);
        if (!File.Exists(finalPath)) return false;

        try
        {
            if (encryptionKey != null)
            {
                await using var stream = await GetAsync(hash, encryptionKey, associatedData, cancellationToken);
                return stream.Length >= 0;
            }
            else
            {
                await using var file = new FileStream(finalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
                var computed = await ContentHash.ComputeAsync(file, cancellationToken);
                return computed == hash;
            }
        }
        catch
        {
            return false;
        }
    }

    public Task<int> CleanupStagingAsync(string jobId)
    {
        var jobStagingDir = Path.Combine(_stagingRoot, SanitizePath(jobId));
        if (!Directory.Exists(jobStagingDir)) return Task.FromResult(0);

        var count = 0;
        foreach (var file in Directory.EnumerateFiles(jobStagingDir))
        {
            try
            {
                File.Delete(file);
                count++;
            }
            catch { /* best effort */ }
        }

        try { Directory.Delete(jobStagingDir, recursive: true); } catch { /* best effort */ }
        return Task.FromResult(count);
    }

    public Task<List<ContentHash>> ListAllObjectsAsync()
    {
        var results = new List<ContentHash>();
        if (!Directory.Exists(_objectsRoot)) return Task.FromResult(results);

        foreach (var prefixDir in Directory.EnumerateDirectories(_objectsRoot))
        {
            foreach (var filePath in Directory.EnumerateFiles(prefixDir))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.Length == 64)
                {
                    try
                    {
                        results.Add(new ContentHash(fileName));
                    }
                    catch { /* ignore invalid names */ }
                }
            }
        }

        return Task.FromResult(results);
    }

    public string GetObjectPhysicalPath(ContentHash hash)
    {
        var prefix = hash.Value[..2];
        return Path.Combine(_objectsRoot, prefix, hash.Value);
    }

    private static string SanitizePath(string input) =>
        string.Concat(input.Split(Path.GetInvalidFileNameChars()));
}
