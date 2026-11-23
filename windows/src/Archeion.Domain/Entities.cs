using Archeion.Contracts;

namespace Archeion.Domain.Entities;

public sealed record DeviceRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string ModelCode { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string Codename { get; init; } = string.Empty;
    public string AndroidVersion { get; init; } = string.Empty;
    public int ApiLevel { get; init; }
    public string BuildFingerprint { get; init; } = string.Empty;
    public string SecurityPatch { get; init; } = string.Empty;
    public DeviceState State { get; set; } = DeviceState.Discovering;
    public DateTimeOffset FirstSeenUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;
    public string IdentityConfidence { get; set; } = "High";
    public string? Notes { get; set; }

    public string Summary => $"{Manufacturer} {Model} (Android {AndroidVersion}, API {ApiLevel})";
}

public sealed record CapabilityAssessment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DeviceId { get; init; }
    public DateTimeOffset AssessedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public int ApiLevel { get; init; }
    public Dictionary<BackupCategory, CapabilityReportItem> CategoryCapabilities { get; init; } = new();

    public CapabilityStatus GetStatus(BackupCategory category) =>
        CategoryCapabilities.TryGetValue(category, out var item) ? item.Status : CapabilityStatus.NotTested;
}

public sealed record CapabilityReportItem(
    BackupCategory Category,
    CapabilityStatus Status,
    string Reason,
    string RequiredPermission,
    long EstimatedItemCount,
    long EstimatedSizeBytes
);

public sealed record BackupPlan
{
    public Guid PlanId { get; init; } = Guid.NewGuid();
    public Guid DeviceId { get; init; }
    public Guid RepositoryId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public HashSet<BackupCategory> SelectedCategories { get; init; } = new();
    public List<string> CustomFolders { get; init; } = new();
    public long EstimatedTotalBytes { get; init; }
    public long EstimatedTotalItems { get; init; }
}

public sealed record SnapshotRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DeviceId { get; init; }
    public Guid RepositoryId { get; init; }
    public Guid? ParentSnapshotId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public SnapshotState State { get; set; } = SnapshotState.InProgress;
    public long TotalBytes { get; set; }
    public long TotalItems { get; set; }
    public string ManifestObjectId { get; set; } = string.Empty;
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
    public List<BackupCategory> IncludedCategories { get; init; } = new();
    public List<BackupItemRecord> Items { get; init; } = new();
}

public sealed record BackupItemRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SnapshotId { get; init; }
    public BackupCategory Category { get; init; }
    public string LogicalPath { get; init; } = string.Empty;
    public string ObjectId { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public string SourceProvider { get; init; } = string.Empty;
    public DateTimeOffset? LastModifiedUtc { get; init; }
}

public sealed record RepositoryMetadata
{
    public Guid RepositoryId { get; init; }
    public string FormatVersion { get; init; } = "1.0.0";
    public string DisplayName { get; set; } = "ARCHEION Personal Library";
    public string RootPath { get; set; } = string.Empty;
    public EncryptionMode EncryptionMode { get; init; } = EncryptionMode.Aes256Gcm;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastOpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsLocked { get; set; }
}
