using System.Text.Json;
using Archeion.Adb;
using Archeion.BackupEngine;
using Archeion.CapabilityEngine;
using Archeion.Contracts;
using Archeion.DeviceDiscovery;
using Archeion.Domain.Entities;
using Archeion.Domain.ValueObjects;
using Archeion.ObjectStore;
using Archeion.Persistence;
using Archeion.RestoreEngine;
using Archeion.Security;
using Archeion.SnapshotComparison;

namespace Archeion.Application;

public sealed record ActiveRepositorySession(
    RepositoryMetadata Metadata,
    byte[]? MasterEncryptionKey,
    RecoveryKey? RecoveryKey
);

public sealed record IntegrityScanResult(
    DateTimeOffset ScannedAtUtc,
    int TotalObjectsChecked,
    int IntactObjectsCount,
    int CorruptedObjectsCount,
    int MissingObjectsCount,
    List<string> Discrepancies
);

public interface IArcheionApplicationService
{
    ActiveRepositorySession? CurrentSession { get; }
    Task<ActiveRepositorySession> CreateRepositoryAsync(string rootPath, string? password = null, string displayName = "ARCHEION Personal Library", CancellationToken cancellationToken = default);
    Task<ActiveRepositorySession> OpenRepositoryAsync(string rootPath, string? password = null, RecoveryKey? recoveryKey = null, CancellationToken cancellationToken = default);
    Task<IntegrityScanResult> RunIntegrityScanAsync(CancellationToken cancellationToken = default);

    Task<List<DiscoveredEndpointResult>> DiscoverDevicesAsync(CancellationToken cancellationToken = default);
    Task<DeviceRecord> RegisterDeviceFromEndpointAsync(DiscoveredEndpointResult endpoint, CancellationToken cancellationToken = default);
    Task RegisterDeviceAsync(DeviceRecord device, CancellationToken cancellationToken = default);
    Task<List<DeviceRecord>> GetRegisteredDevicesAsync(CancellationToken cancellationToken = default);
    Task<CapabilityAssessment> AuditDeviceCapabilitiesAsync(Guid deviceId, Dictionary<string, bool>? livePermissions = null, CancellationToken cancellationToken = default);

    Task<BackupPlan> CreateBackupPlanAsync(Guid deviceId, List<BackupCategory> categories, List<string>? customFolders = null, CancellationToken cancellationToken = default);
    Task<SnapshotRecord> ExecuteBackupAsync(BackupPlan plan, IBackupDataSource dataSource, IProgress<BackupProgressReport>? progress = null, CancellationToken cancellationToken = default);

    Task<List<SnapshotRecord>> GetSnapshotsAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<List<BackupItemRecord>> BrowseSnapshotItemsAsync(Guid snapshotId, BackupCategory? categoryFilter = null, string? search = null, CancellationToken cancellationToken = default);
    Task<bool> ExportItemToFileAsync(Guid snapshotId, Guid itemId, string destinationFilePath, CancellationToken cancellationToken = default);

    Task<RestorePlan> BuildRestorePlanAsync(Guid snapshotId, List<Guid>? itemIds, string destinationDir, RestoreConflictAction conflictAction = RestoreConflictAction.Skip, CancellationToken cancellationToken = default);
    Task<RestoreExecutionResult> ExecuteRestoreAsync(RestorePlan plan, IProgress<RestoreProgressReport>? progress = null, CancellationToken cancellationToken = default);

    Task<SnapshotDiffResult> CompareSnapshotsAsync(Guid baseSnapshotId, Guid targetSnapshotId, CancellationToken cancellationToken = default);
    Task<int> CleanupStagingAsync(string jobId = "manual", CancellationToken cancellationToken = default);
}

public sealed class ArcheionApplicationService : IArcheionApplicationService
{
    private readonly IAdbClient _adbClient;
    private readonly IDeviceDiscoveryService _discoveryService;
    private readonly ICapabilityEvaluator _capabilityEvaluator;
    private readonly IEncryptionService _encryptionService;
    private readonly IKeyDerivationService _kdfService;
    private readonly ISnapshotComparator _comparator;

    private ISqliteConnectionFactory? _connectionFactory;
    private IRepositoryStore? _repositoryStore;
    private IObjectStore? _objectStore;
    private IBackupCoordinator? _backupCoordinator;
    private IRestoreCoordinator? _restoreCoordinator;

    public ActiveRepositorySession? CurrentSession { get; private set; }

    public ArcheionApplicationService(
        IAdbClient? adbClient = null,
        IEncryptionService? encryptionService = null,
        IKeyDerivationService? kdfService = null,
        ICapabilityEvaluator? capabilityEvaluator = null,
        ISnapshotComparator? comparator = null)
    {
        _adbClient = adbClient ?? new AdbClient();
        _discoveryService = new DeviceDiscoveryService(_adbClient);
        _capabilityEvaluator = capabilityEvaluator ?? new CapabilityEvaluator();
        _encryptionService = encryptionService ?? new AesGcmEncryptionService();
        _kdfService = kdfService ?? new Pbkdf2KeyDerivationService();
        _comparator = comparator ?? new SnapshotComparator();
    }

    public async Task<ActiveRepositorySession> CreateRepositoryAsync(
        string rootPath,
        string? password = null,
        string displayName = "ARCHEION Personal Library",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        Directory.CreateDirectory(rootPath);

        var repoId = Guid.NewGuid();
        var encMode = string.IsNullOrEmpty(password) ? EncryptionMode.None : EncryptionMode.Aes256Gcm;

        byte[]? masterKey = null;
        RecoveryKey? recoveryKey = null;

        var keysDir = Path.Combine(rootPath, "keys");
        Directory.CreateDirectory(keysDir);

        if (encMode == EncryptionMode.Aes256Gcm)
        {
            var keyManager = new KeyEnvelopeManager(_encryptionService, _kdfService);
            var (envelope, derivedMasterKey, generatedRecoveryKey) = keyManager.CreateEnvelope(password!);
            masterKey = derivedMasterKey;
            recoveryKey = generatedRecoveryKey;

            var envelopeJson = KeyEnvelopeManager.SerializeEnvelope(envelope);
            await File.WriteAllTextAsync(Path.Combine(keysDir, "key-envelope.json"), envelopeJson, cancellationToken);
        }

        // Initialize SQLite metadata
        var dbPath = Path.Combine(rootPath, "database", "library.sqlite");
        _connectionFactory = new SqliteConnectionFactory(dbPath);
        var migrator = new DatabaseMigrator(_connectionFactory);
        migrator.MigrateToLatest();

        _repositoryStore = new RepositoryStore(_connectionFactory);
        _objectStore = new ContentAddressableStore(rootPath, _encryptionService);
        _backupCoordinator = new BackupCoordinator(rootPath, _objectStore, _repositoryStore);
        _restoreCoordinator = new RestoreCoordinator(_objectStore, _repositoryStore);

        var metadata = new RepositoryMetadata
        {
            RepositoryId = repoId,
            FormatVersion = "1.0.0",
            DisplayName = displayName,
            RootPath = rootPath,
            EncryptionMode = encMode,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastOpenedAtUtc = DateTimeOffset.UtcNow
        };

        await _repositoryStore.InitializeRepositoryAsync(metadata, cancellationToken);

        // Write repository.json root descriptor
        var repoDescriptor = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(rootPath, "repository.json"), repoDescriptor, cancellationToken);

        CurrentSession = new ActiveRepositorySession(metadata, masterKey, recoveryKey);
        return CurrentSession;
    }

    public async Task<ActiveRepositorySession> OpenRepositoryAsync(
        string rootPath,
        string? password = null,
        RecoveryKey? recoveryKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        var repoDescriptorPath = Path.Combine(rootPath, "repository.json");
        if (!File.Exists(repoDescriptorPath))
        {
            throw new FileNotFoundException("Invalid ARCHEION repository: 'repository.json' not found.", repoDescriptorPath);
        }

        var dbPath = Path.Combine(rootPath, "database", "library.sqlite");
        _connectionFactory = new SqliteConnectionFactory(dbPath);
        var migrator = new DatabaseMigrator(_connectionFactory);
        migrator.MigrateToLatest();

        _repositoryStore = new RepositoryStore(_connectionFactory);
        _objectStore = new ContentAddressableStore(rootPath, _encryptionService);
        _backupCoordinator = new BackupCoordinator(rootPath, _objectStore, _repositoryStore);
        _restoreCoordinator = new RestoreCoordinator(_objectStore, _repositoryStore);

        var metadata = await _repositoryStore.GetRepositoryMetadataAsync(cancellationToken)
            ?? throw new InvalidOperationException("Repository metadata record not found in database.");

        byte[]? masterKey = null;

        if (metadata.EncryptionMode == EncryptionMode.Aes256Gcm)
        {
            var envelopePath = Path.Combine(rootPath, "keys", "key-envelope.json");
            if (!File.Exists(envelopePath))
            {
                throw new FileNotFoundException("Key envelope file not found for encrypted repository.", envelopePath);
            }

            var envelopeJson = await File.ReadAllTextAsync(envelopePath, cancellationToken);
            var envelope = KeyEnvelopeManager.DeserializeEnvelope(envelopeJson);
            var keyManager = new KeyEnvelopeManager(_encryptionService, _kdfService);

            if (!string.IsNullOrEmpty(password))
            {
                masterKey = keyManager.UnlockWithPassword(envelope, password);
            }
            else if (recoveryKey != null)
            {
                masterKey = keyManager.UnlockWithRecoveryKey(envelope, recoveryKey);
            }
            else
            {
                throw new InvalidOperationException("Encrypted repository requires password or recovery key to unlock.");
            }
        }

        metadata.LastOpenedAtUtc = DateTimeOffset.UtcNow;
        await _repositoryStore.UpdateLastOpenedAsync(metadata.RepositoryId, metadata.LastOpenedAtUtc, cancellationToken);

        CurrentSession = new ActiveRepositorySession(metadata, masterKey, recoveryKey);
        return CurrentSession;
    }

    public async Task<IntegrityScanResult> RunIntegrityScanAsync(CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        var allTrackedObjectIds = await _repositoryStore!.GetAllTrackedObjectIdsAsync(cancellationToken);
        int intact = 0;
        int corrupted = 0;
        int missing = 0;
        var discrepancies = new List<string>();

        foreach (var objId in allTrackedObjectIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hash = new ContentHash(objId);

            if (!await _objectStore!.ExistsAsync(hash))
            {
                missing++;
                discrepancies.Add($"Missing object: {objId}");
                continue;
            }

            var isIntact = await _objectStore.VerifyObjectIntegrityAsync(hash, CurrentSession!.MasterEncryptionKey, cancellationToken: cancellationToken);
            if (isIntact)
            {
                intact++;
            }
            else
            {
                corrupted++;
                discrepancies.Add($"Corrupted or checksum mismatch: {objId}");
            }
        }

        return new IntegrityScanResult(
            ScannedAtUtc: DateTimeOffset.UtcNow,
            TotalObjectsChecked: allTrackedObjectIds.Count,
            IntactObjectsCount: intact,
            CorruptedObjectsCount: corrupted,
            MissingObjectsCount: missing,
            Discrepancies: discrepancies
        );
    }

    public Task<List<DiscoveredEndpointResult>> DiscoverDevicesAsync(CancellationToken cancellationToken = default) =>
        _discoveryService.DiscoverEndpointsAsync(cancellationToken);

    public async Task<DeviceRecord> RegisterDeviceFromEndpointAsync(DiscoveredEndpointResult endpoint, CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        var device = await _discoveryService.ResolveDeviceIdentityAsync(endpoint, cancellationToken);
        await _repositoryStore!.RegisterOrUpdateDeviceAsync(device, cancellationToken);
        return device;
    }

    public async Task RegisterDeviceAsync(DeviceRecord device, CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        ArgumentNullException.ThrowIfNull(device);
        await _repositoryStore!.RegisterOrUpdateDeviceAsync(device, cancellationToken);
    }

    public Task<List<DeviceRecord>> GetRegisteredDevicesAsync(CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        return _repositoryStore!.GetAllDevicesAsync(cancellationToken);
    }

    public async Task<CapabilityAssessment> AuditDeviceCapabilitiesAsync(
        Guid deviceId,
        Dictionary<string, bool>? livePermissions = null,
        CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        var device = await _repositoryStore!.GetDeviceAsync(deviceId, cancellationToken)
            ?? throw new ArgumentException($"Device {deviceId} not found.", nameof(deviceId));

        var assessment = _capabilityEvaluator.Evaluate(device, livePermissions);
        await _repositoryStore.SaveCapabilityAssessmentAsync(assessment, cancellationToken);
        return assessment;
    }

    public async Task<BackupPlan> CreateBackupPlanAsync(
        Guid deviceId,
        List<BackupCategory> categories,
        List<string>? customFolders = null,
        CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        var device = await _repositoryStore!.GetDeviceAsync(deviceId, cancellationToken)
            ?? throw new ArgumentException($"Device {deviceId} not found.", nameof(deviceId));

        return new BackupPlan
        {
            DeviceId = device.Id,
            RepositoryId = CurrentSession!.Metadata.RepositoryId,
            SelectedCategories = categories.ToHashSet(),
            CustomFolders = customFolders ?? new List<string>()
        };
    }

    public Task<SnapshotRecord> ExecuteBackupAsync(
        BackupPlan plan,
        IBackupDataSource dataSource,
        IProgress<BackupProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        return _backupCoordinator!.ExecuteBackupAsync(
            plan,
            dataSource,
            CurrentSession!.MasterEncryptionKey,
            progress,
            cancellationToken
        );
    }

    public Task<List<SnapshotRecord>> GetSnapshotsAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        return _repositoryStore!.GetSnapshotsForDeviceAsync(deviceId, cancellationToken);
    }

    public async Task<List<BackupItemRecord>> BrowseSnapshotItemsAsync(
        Guid snapshotId,
        BackupCategory? categoryFilter = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        var items = await _repositoryStore!.GetBackupItemsAsync(snapshotId, cancellationToken);

        if (categoryFilter.HasValue)
        {
            items = items.Where(i => i.Category == categoryFilter.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            items = items.Where(i => i.LogicalPath.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return items;
    }

    public async Task<bool> ExportItemToFileAsync(
        Guid snapshotId,
        Guid itemId,
        string destinationFilePath,
        CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        var items = await _repositoryStore!.GetBackupItemsAsync(snapshotId, cancellationToken);
        var item = items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new ArgumentException($"Item {itemId} not found in snapshot {snapshotId}.", nameof(itemId));

        var dir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var objectHash = new ContentHash(item.ObjectId);
        await using var sourceStream = await _objectStore!.GetAsync(objectHash, CurrentSession!.MasterEncryptionKey, cancellationToken: cancellationToken);
        await using var destStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, useAsync: true);

        await sourceStream.CopyToAsync(destStream, cancellationToken);

        // Verify exported file hash
        destStream.Position = 0;
        var computedHash = await ContentHash.ComputeAsync(destStream, cancellationToken);
        return computedHash.Value == item.Sha256;
    }

    public async Task<RestorePlan> BuildRestorePlanAsync(
        Guid snapshotId,
        List<Guid>? itemIds,
        string destinationDir,
        RestoreConflictAction conflictAction = RestoreConflictAction.Skip,
        CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        var snapshot = await _repositoryStore!.GetSnapshotAsync(snapshotId, cancellationToken)
            ?? throw new ArgumentException($"Snapshot {snapshotId} not found.", nameof(snapshotId));

        return await _restoreCoordinator!.BuildRestorePlanAsync(snapshot, itemIds, destinationDir, conflictAction, cancellationToken);
    }

    public Task<RestoreExecutionResult> ExecuteRestoreAsync(
        RestorePlan plan,
        IProgress<RestoreProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        return _restoreCoordinator!.ExecuteRestoreAsync(
            plan,
            CurrentSession!.MasterEncryptionKey,
            progress,
            cancellationToken
        );
    }

    public async Task<SnapshotDiffResult> CompareSnapshotsAsync(
        Guid baseSnapshotId,
        Guid targetSnapshotId,
        CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        var baseSnap = await _repositoryStore!.GetSnapshotAsync(baseSnapshotId, cancellationToken)
            ?? throw new ArgumentException($"Base snapshot {baseSnapshotId} not found.", nameof(baseSnapshotId));

        var targetSnap = await _repositoryStore.GetSnapshotAsync(targetSnapshotId, cancellationToken)
            ?? throw new ArgumentException($"Target snapshot {targetSnapshotId} not found.", nameof(targetSnapshotId));

        return _comparator.Compare(baseSnap, targetSnap);
    }

    public Task<int> CleanupStagingAsync(string jobId = "manual", CancellationToken cancellationToken = default)
    {
        EnsureSessionOpen();
        return _objectStore?.CleanupStagingAsync(jobId) ?? Task.FromResult(0);
    }

    private void EnsureSessionOpen()
    {
        if (CurrentSession == null || _repositoryStore == null || _objectStore == null)
        {
            throw new InvalidOperationException("No repository session is currently open. Please create or open a repository first.");
        }
    }
}
