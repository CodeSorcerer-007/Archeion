using Archeion.Contracts;
using Archeion.Domain.Entities;
using Archeion.Domain.ValueObjects;
using Archeion.ObjectStore;
using Archeion.Persistence;

namespace Archeion.RestoreEngine;

public sealed record RestorePlan
{
    public Guid PlanId { get; init; } = Guid.NewGuid();
    public Guid SourceSnapshotId { get; init; }
    public string DestinationDirectory { get; init; } = string.Empty;
    public RestoreConflictAction ConflictAction { get; init; } = RestoreConflictAction.Skip;
    public List<RestoreItemPlan> Items { get; init; } = new();
}

public sealed record RestoreItemPlan(
    BackupItemRecord Item,
    string TargetPath,
    bool ExistsAtDestination,
    RestoreConflictAction Action
);

public sealed record RestoreExecutionResult(
    Guid PlanId,
    int TotalItems,
    int RestoredCount,
    int SkippedCount,
    int FailedCount,
    long RestoredBytes,
    List<string> Errors
);

public sealed record RestoreProgressReport(
    string CurrentItemName,
    int ProcessedItems,
    int TotalItems,
    long RestoredBytes
);

public interface IRestoreCoordinator
{
    Task<RestorePlan> BuildRestorePlanAsync(
        SnapshotRecord snapshot,
        List<Guid>? selectedItemIds,
        string destinationDirectory,
        RestoreConflictAction conflictAction = RestoreConflictAction.Skip,
        CancellationToken cancellationToken = default);

    Task<RestoreExecutionResult> ExecuteRestoreAsync(
        RestorePlan plan,
        byte[]? encryptionKey = null,
        IProgress<RestoreProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class RestoreCoordinator : IRestoreCoordinator
{
    private readonly IObjectStore _objectStore;
    private readonly IRepositoryStore _repositoryStore;

    public RestoreCoordinator(IObjectStore objectStore, IRepositoryStore repositoryStore)
    {
        _objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
        _repositoryStore = repositoryStore ?? throw new ArgumentNullException(nameof(repositoryStore));
    }

    public Task<RestorePlan> BuildRestorePlanAsync(
        SnapshotRecord snapshot,
        List<Guid>? selectedItemIds,
        string destinationDirectory,
        RestoreConflictAction conflictAction = RestoreConflictAction.Skip,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            throw new ArgumentException("Destination directory cannot be empty.", nameof(destinationDirectory));
        }

        Directory.CreateDirectory(destinationDirectory);

        var candidateItems = selectedItemIds == null || selectedItemIds.Count == 0
            ? snapshot.Items
            : snapshot.Items.Where(i => selectedItemIds.Contains(i.Id)).ToList();

        var plans = new List<RestoreItemPlan>();

        foreach (var item in candidateItems)
        {
            var relativeStoragePath = new StoragePath(item.LogicalPath);
            var safeRelativePath = relativeStoragePath.NormalizedPath.Replace('/', Path.DirectorySeparatorChar);
            var targetPath = Path.Combine(destinationDirectory, safeRelativePath);
            var exists = File.Exists(targetPath);

            var action = conflictAction;
            if (exists && conflictAction == RestoreConflictAction.Rename)
            {
                var dir = Path.GetDirectoryName(targetPath)!;
                var fileNameNoExt = Path.GetFileNameWithoutExtension(targetPath);
                var ext = Path.GetExtension(targetPath);
                targetPath = Path.Combine(dir, $"{fileNameNoExt}_restored_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}{ext}");
                exists = false;
            }

            plans.Add(new RestoreItemPlan(item, targetPath, exists, action));
        }

        return Task.FromResult(new RestorePlan
        {
            SourceSnapshotId = snapshot.Id,
            DestinationDirectory = destinationDirectory,
            ConflictAction = conflictAction,
            Items = plans
        });
    }

    public async Task<RestoreExecutionResult> ExecuteRestoreAsync(
        RestorePlan plan,
        byte[]? encryptionKey = null,
        IProgress<RestoreProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        int restored = 0;
        int skipped = 0;
        int failed = 0;
        long bytesRestored = 0;
        var errors = new List<string>();

        for (int i = 0; i < plan.Items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var itemPlan = plan.Items[i];

            progress?.Report(new RestoreProgressReport(
                CurrentItemName: itemPlan.Item.LogicalPath,
                ProcessedItems: i,
                TotalItems: plan.Items.Count,
                RestoredBytes: bytesRestored
            ));

            if (itemPlan.ExistsAtDestination && itemPlan.Action == RestoreConflictAction.Skip)
            {
                skipped++;
                continue;
            }

            try
            {
                var targetDir = Path.GetDirectoryName(itemPlan.TargetPath)!;
                Directory.CreateDirectory(targetDir);

                var objectHash = new ContentHash(itemPlan.Item.ObjectId);
                await using var sourceStream = await _objectStore.GetAsync(objectHash, encryptionKey, cancellationToken: cancellationToken);
                await using var destStream = new FileStream(itemPlan.TargetPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, useAsync: true);

                await sourceStream.CopyToAsync(destStream, cancellationToken);

                // Verify restored file hash matches
                destStream.Position = 0;
                var computedHash = await ContentHash.ComputeAsync(destStream, cancellationToken);
                if (computedHash.Value != itemPlan.Item.Sha256)
                {
                    failed++;
                    errors.Add($"Verification failed for {itemPlan.Item.LogicalPath}: Expected {itemPlan.Item.Sha256}, got {computedHash.Value}");
                    continue;
                }

                restored++;
                bytesRestored += itemPlan.Item.SizeBytes;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"Error restoring {itemPlan.Item.LogicalPath}: {ex.Message}");
            }
        }

        progress?.Report(new RestoreProgressReport("Completed", plan.Items.Count, plan.Items.Count, bytesRestored));

        return new RestoreExecutionResult(
            PlanId: plan.PlanId,
            TotalItems: plan.Items.Count,
            RestoredCount: restored,
            SkippedCount: skipped,
            FailedCount: failed,
            RestoredBytes: bytesRestored,
            Errors: errors
        );
    }
}
