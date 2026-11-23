using System.Security.Cryptography;
using System.Text;
using Archeion.Contracts;
using Archeion.Domain.Entities;
using Archeion.Domain.ValueObjects;
using Archeion.ObjectStore;
using Archeion.Persistence;
using Archeion.Security;
using Xunit;

namespace Archeion.StorageTests;

public class StorageTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _dbPath;
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly DatabaseMigrator _migrator;
    private readonly RepositoryStore _store;
    private readonly AesGcmEncryptionService _encryptionService;
    private readonly ContentAddressableStore _objectStore;

    public StorageTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "ArcheionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _dbPath = Path.Combine(_testDir, "database", "library.sqlite");

        _connectionFactory = new SqliteConnectionFactory(_dbPath);
        _migrator = new DatabaseMigrator(_connectionFactory);
        _migrator.MigrateToLatest();

        _store = new RepositoryStore(_connectionFactory);
        _encryptionService = new AesGcmEncryptionService();
        _objectStore = new ContentAddressableStore(_testDir, _encryptionService);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { /* best effort */ }
    }

    [Fact]
    public async Task Repository_InitializeAndRetrieve_Matches()
    {
        var meta = new RepositoryMetadata
        {
            RepositoryId = Guid.NewGuid(),
            DisplayName = "Test Repo",
            RootPath = _testDir,
            EncryptionMode = EncryptionMode.Aes256Gcm,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastOpenedAtUtc = DateTimeOffset.UtcNow
        };

        await _store.InitializeRepositoryAsync(meta);
        var retrieved = await _store.GetRepositoryMetadataAsync();

        Assert.NotNull(retrieved);
        Assert.Equal(meta.RepositoryId, retrieved.RepositoryId);
        Assert.Equal(meta.DisplayName, retrieved.DisplayName);
        Assert.Equal(meta.EncryptionMode, retrieved.EncryptionMode);
    }

    [Fact]
    public async Task Device_RegisterAndRetrieve_Succeeds()
    {
        var device = new DeviceRecord
        {
            Id = Guid.NewGuid(),
            DisplayName = "Pixel 8 Pro",
            Manufacturer = "Google",
            Brand = "Google",
            Model = "Pixel 8 Pro",
            ModelCode = "GC3VE",
            Product = "husky",
            Codename = "husky",
            AndroidVersion = "15",
            ApiLevel = 35,
            BuildFingerprint = "google/husky/husky:15/AP2A.240805.005/12025142:user/release-keys",
            SecurityPatch = "2024-08-05",
            State = DeviceState.Connected,
            IdentityConfidence = "High"
        };

        await _store.RegisterOrUpdateDeviceAsync(device);
        var retrieved = await _store.GetDeviceAsync(device.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(device.Model, retrieved.Model);
        Assert.Equal(device.ApiLevel, retrieved.ApiLevel);
        Assert.Equal(DeviceState.Connected, retrieved.State);
    }

    [Fact]
    public async Task ObjectStore_Put_Get_Verify_Unencrypted_Succeeds()
    {
        var data = Encoding.UTF8.GetBytes("Unencrypted test payload for ARCHEION object store");
        using var stream = new MemoryStream(data);

        var hash = await _objectStore.PutAsync(stream, "job-1");
        Assert.NotNull(hash.Value);

        var exists = await _objectStore.ExistsAsync(hash);
        Assert.True(exists);

        var isIntact = await _objectStore.VerifyObjectIntegrityAsync(hash);
        Assert.True(isIntact);

        await using var retrievedStream = await _objectStore.GetAsync(hash);
        using var ms = new MemoryStream();
        await retrievedStream.CopyToAsync(ms);
        Assert.Equal(data, ms.ToArray());
    }

    [Fact]
    public async Task ObjectStore_Put_Get_Verify_Encrypted_Succeeds()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var aad = Encoding.UTF8.GetBytes("TestAAD");
        var data = Encoding.UTF8.GetBytes("Encrypted high security photo object payload");
        using var stream = new MemoryStream(data);

        var hash = await _objectStore.PutAsync(stream, "job-2", encryptionKey: key, associatedData: aad);
        Assert.NotNull(hash.Value);

        var isIntact = await _objectStore.VerifyObjectIntegrityAsync(hash, encryptionKey: key, associatedData: aad);
        Assert.True(isIntact);

        await using var retrievedStream = await _objectStore.GetAsync(hash, encryptionKey: key, associatedData: aad);
        using var ms = new MemoryStream();
        await retrievedStream.CopyToAsync(ms);
        Assert.Equal(data, ms.ToArray());
    }

    [Fact]
    public async Task Snapshot_SaveAndRetrieve_WithItems_Succeeds()
    {
        var deviceId = Guid.NewGuid();
        var repoId = Guid.NewGuid();

        await _store.InitializeRepositoryAsync(new RepositoryMetadata
        {
            RepositoryId = repoId,
            RootPath = _testDir
        });

        await _store.RegisterOrUpdateDeviceAsync(new DeviceRecord
        {
            Id = deviceId,
            DisplayName = "Test Phone",
            Manufacturer = "Samsung",
            Model = "Galaxy S24",
            AndroidVersion = "14",
            ApiLevel = 34
        });

        var snapshot = new SnapshotRecord
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            RepositoryId = repoId,
            State = SnapshotState.Completed,
            TotalBytes = 1048576,
            TotalItems = 1,
            ManifestObjectId = "manifest-test-obj",
            VerificationStatus = VerificationStatus.Verified,
            Items =
            {
                new BackupItemRecord
                {
                    Id = Guid.NewGuid(),
                    Category = BackupCategory.Photos,
                    LogicalPath = "DCIM/Camera/IMG_20260101.jpg",
                    ObjectId = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    SizeBytes = 1048576,
                    Sha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    SourceProvider = "MediaStore"
                }
            }
        };

        await _store.SaveSnapshotAsync(snapshot);

        var retrieved = await _store.GetSnapshotAsync(snapshot.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(snapshot.TotalBytes, retrieved.TotalBytes);
        Assert.Single(retrieved.Items);
        Assert.Equal("DCIM/Camera/IMG_20260101.jpg", retrieved.Items[0].LogicalPath);
    }
}
