using System.Text.Json.Serialization;

namespace Archeion.Contracts;

public sealed record SnapshotManifestDto
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = "1.0.0";

    [JsonPropertyName("snapshotId")]
    public Guid SnapshotId { get; init; }

    [JsonPropertyName("deviceId")]
    public Guid DeviceId { get; init; }

    [JsonPropertyName("repositoryId")]
    public Guid RepositoryId { get; init; }

    [JsonPropertyName("parentSnapshotId")]
    public Guid? ParentSnapshotId { get; init; }

    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; init; }

    [JsonPropertyName("state")]
    public SnapshotState State { get; init; }

    [JsonPropertyName("totalBytes")]
    public long TotalBytes { get; init; }

    [JsonPropertyName("totalItems")]
    public long TotalItems { get; init; }

    [JsonPropertyName("categories")]
    public List<BackupCategory> Categories { get; init; } = new();

    [JsonPropertyName("items")]
    public List<ManifestItemDto> Items { get; init; } = new();
}

public sealed record ManifestItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("category")]
    public BackupCategory Category { get; init; }

    [JsonPropertyName("logicalPath")]
    public string LogicalPath { get; init; } = string.Empty;

    [JsonPropertyName("objectId")]
    public string ObjectId { get; init; } = string.Empty;

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [JsonPropertyName("sourceProvider")]
    public string SourceProvider { get; init; } = string.Empty;

    [JsonPropertyName("lastModifiedUtc")]
    public DateTimeOffset? LastModifiedUtc { get; init; }
}

public sealed record DeviceProfileDto
{
    public Guid DeviceId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
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
    public DateTimeOffset FirstSeenUtc { get; init; }
    public DateTimeOffset LastSeenUtc { get; init; }
}
