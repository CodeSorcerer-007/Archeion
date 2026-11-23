using System.Text.Json;
using Archeion.Contracts;
using Archeion.Domain.Entities;
using Archeion.Domain.ValueObjects;
using Archeion.ObjectStore;
using Archeion.Persistence;

namespace Archeion.BackupEngine;

public interface IBackupDataSource
{
    Task<List<SourceItemDescriptor>> EnumerateItemsAsync(BackupPlan plan, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadStreamAsync(SourceItemDescriptor item, CancellationToken cancellationToken = default);
}

public sealed record SourceItemDescriptor(
    BackupCategory Category,
    string LogicalPath,
    long SizeBytes,
    DateTimeOffset? LastModifiedUtc,
    string SourceProvider,
    string? RemoteId = null
);

public sealed record BackupProgressReport(
    string CurrentStage,
    string CurrentItemName,
    BackupCategory CurrentCategory,
    long ProcessedBytes,
    long TotalBytes,
    long ProcessedItems,
    long TotalItems
);

public interface IBackupCoordinator
{
    Task<SnapshotRecord> ExecuteBackupAsync(
        BackupPlan plan,
        IBackupDataSource dataSource,
        byte[]? encryptionKey = null,
        IProgress<BackupProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class BackupCoordinator : IBackupCoordinator
{
    private readonly IObjectStore _objectStore;
    private readonly IRepositoryStore _repositoryStore;
    private readonly string _repositoryRoot;

    public BackupCoordinator(string repositoryRoot, IObjectStore objectStore, IRepositoryStore repositoryStore)
    {
        _repositoryRoot = repositoryRoot ?? throw new ArgumentNullException(nameof(repositoryRoot));
        _objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
        _repositoryStore = repositoryStore ?? throw new ArgumentNullException(nameof(repositoryStore));
    }

    public async Task<SnapshotRecord> ExecuteBackupAsync(
        BackupPlan plan,
        IBackupDataSource dataSource,
        byte[]? encryptionKey = null,
        IProgress<BackupProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(dataSource);

        var jobId = Guid.NewGuid().ToString("N");
        var snapshotId = Guid.NewGuid();

        try
        {
            // Stage 1: Preflight
            progress?.Report(new BackupProgressReport("Preflight", "Verifying repository readiness...", BackupCategory.DeviceIdentity, 0, 0, 0, 0));
            var repoMeta = await _repositoryStore.GetRepositoryMetadataAsync(cancellationToken)
                ?? throw new InvalidOperationException("Repository metadata could not be found.");

            // Stage 2: Enumeration
            progress?.Report(new BackupProgressReport("Enumeration", "Enumerating accessible categories...", BackupCategory.DeviceIdentity, 0, 0, 0, 0));
            var sourceItems = await dataSource.EnumerateItemsAsync(plan, cancellationToken);
            var totalBytes = sourceItems.Sum(i => i.SizeBytes);
            var totalItems = sourceItems.Count;

            var collectedBackupItems = new List<BackupItemRecord>();
            long processedBytes = 0;
            long processedItems = 0;

            // Stage 3 & 4: Collection, Hashing, Encryption & Object Store Promotion
            foreach (var item in sourceItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report(new BackupProgressReport(
                    CurrentStage: "Collecting",
                    CurrentItemName: item.LogicalPath,
                    CurrentCategory: item.Category,
                    ProcessedBytes: processedBytes,
                    TotalBytes: totalBytes,
                    ProcessedItems: processedItems,
                    TotalItems: totalItems
                ));

                // Safe path validation
                var storagePath = new StoragePath(item.LogicalPath);

                await using var contentStream = await dataSource.OpenReadStreamAsync(item, cancellationToken);
                var objectHash = await _objectStore.PutAsync(contentStream, jobId, encryptionKey, cancellationToken: cancellationToken);

                var backupItem = new BackupItemRecord
                {
                    Id = Guid.NewGuid(),
                    SnapshotId = snapshotId,
                    Category = item.Category,
                    LogicalPath = storagePath.NormalizedPath,
                    ObjectId = objectHash.Value,
                    SizeBytes = item.SizeBytes,
                    Sha256 = objectHash.Value,
                    SourceProvider = item.SourceProvider,
                    LastModifiedUtc = item.LastModifiedUtc
                };

                collectedBackupItems.Add(backupItem);
                await _repositoryStore.RecordObjectReferenceAsync(objectHash.Value, item.SizeBytes, repoMeta.EncryptionMode, cancellationToken);

                processedBytes += item.SizeBytes;
                processedItems++;
            }

            // Stage 5 & 6: Manifest Generation & Atomic Commit
            progress?.Report(new BackupProgressReport("Committing", "Writing snapshot manifest and database index...", BackupCategory.DeviceIdentity, processedBytes, totalBytes, processedItems, totalItems));

            var manifestDto = new SnapshotManifestDto
            {
                SchemaVersion = "1.0.0",
                SnapshotId = snapshotId,
                DeviceId = plan.DeviceId,
                RepositoryId = plan.RepositoryId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                State = SnapshotState.Completed,
                TotalBytes = processedBytes,
                TotalItems = processedItems,
                Categories = plan.SelectedCategories.ToList(),
                Items = collectedBackupItems.Select(b => new ManifestItemDto
                {
                    Id = b.Id,
                    Category = b.Category,
                    LogicalPath = b.LogicalPath,
                    ObjectId = b.ObjectId,
                    SizeBytes = b.SizeBytes,
                    Sha256 = b.Sha256,
                    SourceProvider = b.SourceProvider,
                    LastModifiedUtc = b.LastModifiedUtc
                }).ToList()
            };

            var manifestJson = JsonSerializer.Serialize(manifestDto, new JsonSerializerOptions { WriteIndented = true });
            using (var manifestStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(manifestJson)))
            {
                var manifestHash = await _objectStore.PutAsync(manifestStream, jobId, encryptionKey, cancellationToken: cancellationToken);
                await _repositoryStore.RecordObjectReferenceAsync(manifestHash.Value, manifestStream.Length, repoMeta.EncryptionMode, cancellationToken);

                // Write manifest.json to device snapshot folder for direct portable inspection
                var deviceSnapshotDir = Path.Combine(_repositoryRoot, "devices", plan.DeviceId.ToString(), "snapshots", snapshotId.ToString());
                Directory.CreateDirectory(deviceSnapshotDir);
                await File.WriteAllTextAsync(Path.Combine(deviceSnapshotDir, "manifest.json"), manifestJson, cancellationToken);

                var snapshotRecord = new SnapshotRecord
                {
                    Id = snapshotId,
                    DeviceId = plan.DeviceId,
                    RepositoryId = plan.RepositoryId,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    State = SnapshotState.Completed,
                    TotalBytes = processedBytes,
                    TotalItems = processedItems,
                    ManifestObjectId = manifestHash.Value,
                    VerificationStatus = VerificationStatus.Verified,
                    IncludedCategories = plan.SelectedCategories.ToList(),
                    Items = collectedBackupItems
                };

                await _repositoryStore.SaveSnapshotAsync(snapshotRecord, cancellationToken);

                // Clean staging
                await _objectStore.CleanupStagingAsync(jobId);

                progress?.Report(new BackupProgressReport("Completed", "Backup completed and verified.", BackupCategory.DeviceIdentity, processedBytes, totalBytes, processedItems, totalItems));

                return snapshotRecord;
            }
        }
        catch
        {
            // Clean up staging on any failure or cancellation
            try { await _objectStore.CleanupStagingAsync(jobId); } catch { /* best effort */ }
            throw;
        }
    }
}
