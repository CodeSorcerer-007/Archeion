using Archeion.Contracts;
using Archeion.Domain.Entities;

namespace Archeion.SnapshotComparison;

public sealed record SnapshotDiffResult
{
    public Guid BaseSnapshotId { get; init; }
    public Guid TargetSnapshotId { get; init; }
    public List<SnapshotItemDiff> ItemDifferences { get; init; } = new();
    public int AddedCount => ItemDifferences.Count(d => d.ChangeType == ChangeType.Added);
    public int RemovedCount => ItemDifferences.Count(d => d.ChangeType == ChangeType.RemovedFromSource);
    public int ModifiedCount => ItemDifferences.Count(d => d.ChangeType == ChangeType.Modified);
    public int UnchangedCount => ItemDifferences.Count(d => d.ChangeType == ChangeType.Unchanged);
    public int RenamedCount => ItemDifferences.Count(d => d.ChangeType == ChangeType.Renamed);

    public long AddedBytes => ItemDifferences.Where(d => d.ChangeType == ChangeType.Added).Sum(d => d.TargetItem?.SizeBytes ?? 0);
    public long ModifiedBytes => ItemDifferences.Where(d => d.ChangeType == ChangeType.Modified).Sum(d => d.TargetItem?.SizeBytes ?? 0);
    public long RemovedBytes => ItemDifferences.Where(d => d.ChangeType == ChangeType.RemovedFromSource).Sum(d => d.BaseItem?.SizeBytes ?? 0);
}

public sealed record SnapshotItemDiff(
    ChangeType ChangeType,
    BackupCategory Category,
    string LogicalPath,
    BackupItemRecord? BaseItem,
    BackupItemRecord? TargetItem
);

public interface ISnapshotComparator
{
    SnapshotDiffResult Compare(SnapshotRecord baseSnapshot, SnapshotRecord targetSnapshot);
}

public sealed class SnapshotComparator : ISnapshotComparator
{
    public SnapshotDiffResult Compare(SnapshotRecord baseSnapshot, SnapshotRecord targetSnapshot)
    {
        ArgumentNullException.ThrowIfNull(baseSnapshot);
        ArgumentNullException.ThrowIfNull(targetSnapshot);

        var baseByPath = baseSnapshot.Items.ToDictionary(i => i.LogicalPath, StringComparer.OrdinalIgnoreCase);
        var targetByPath = targetSnapshot.Items.ToDictionary(i => i.LogicalPath, StringComparer.OrdinalIgnoreCase);

        var diffs = new List<SnapshotItemDiff>();

        // Check target items against base items
        foreach (var (path, targetItem) in targetByPath)
        {
            if (baseByPath.TryGetValue(path, out var baseItem))
            {
                if (baseItem.Sha256 == targetItem.Sha256)
                {
                    diffs.Add(new SnapshotItemDiff(ChangeType.Unchanged, targetItem.Category, path, baseItem, targetItem));
                }
                else
                {
                    diffs.Add(new SnapshotItemDiff(ChangeType.Modified, targetItem.Category, path, baseItem, targetItem));
                }
            }
            else
            {
                // Check if it's a rename of an item from base with identical hash
                var renameCandidate = baseSnapshot.Items.FirstOrDefault(b => b.Sha256 == targetItem.Sha256 && !targetByPath.ContainsKey(b.LogicalPath));
                if (renameCandidate != null)
                {
                    diffs.Add(new SnapshotItemDiff(ChangeType.Renamed, targetItem.Category, path, renameCandidate, targetItem));
                }
                else
                {
                    diffs.Add(new SnapshotItemDiff(ChangeType.Added, targetItem.Category, path, null, targetItem));
                }
            }
        }

        // Check for items in base that are absent in target
        foreach (var (path, baseItem) in baseByPath)
        {
            if (!targetByPath.ContainsKey(path))
            {
                // If this base item was accounted for as a renamed target, don't duplicate as removed
                var wasRenamed = diffs.Any(d => d.ChangeType == ChangeType.Renamed && d.BaseItem?.LogicalPath == path);
                if (!wasRenamed)
                {
                    diffs.Add(new SnapshotItemDiff(ChangeType.RemovedFromSource, baseItem.Category, path, baseItem, null));
                }
            }
        }

        return new SnapshotDiffResult
        {
            BaseSnapshotId = baseSnapshot.Id,
            TargetSnapshotId = targetSnapshot.Id,
            ItemDifferences = diffs.OrderBy(d => d.LogicalPath).ToList()
        };
    }
}
