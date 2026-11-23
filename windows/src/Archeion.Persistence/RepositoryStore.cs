using System.Text.Json;
using Archeion.Contracts;
using Archeion.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace Archeion.Persistence;

public interface IRepositoryStore
{
    Task InitializeRepositoryAsync(RepositoryMetadata metadata, CancellationToken cancellationToken = default);
    Task<RepositoryMetadata?> GetRepositoryMetadataAsync(CancellationToken cancellationToken = default);
    Task UpdateLastOpenedAsync(Guid repositoryId, DateTimeOffset openedAt, CancellationToken cancellationToken = default);
    Task RegisterOrUpdateDeviceAsync(DeviceRecord device, CancellationToken cancellationToken = default);
    Task<DeviceRecord?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<List<DeviceRecord>> GetAllDevicesAsync(CancellationToken cancellationToken = default);
    Task SaveCapabilityAssessmentAsync(CapabilityAssessment assessment, CancellationToken cancellationToken = default);
    Task<CapabilityAssessment?> GetLatestCapabilityAssessmentAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(SnapshotRecord snapshot, CancellationToken cancellationToken = default);
    Task<SnapshotRecord?> GetSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default);
    Task<List<SnapshotRecord>> GetSnapshotsForDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<List<BackupItemRecord>> GetBackupItemsAsync(Guid snapshotId, CancellationToken cancellationToken = default);
    Task UpdateSnapshotVerificationAsync(Guid snapshotId, VerificationStatus status, CancellationToken cancellationToken = default);
    Task RecordObjectReferenceAsync(string objectId, long sizeBytes, EncryptionMode mode, CancellationToken cancellationToken = default);
    Task<List<string>> GetAllTrackedObjectIdsAsync(CancellationToken cancellationToken = default);
}

public sealed class RepositoryStore : IRepositoryStore
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public RepositoryStore(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeRepositoryAsync(RepositoryMetadata metadata, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO repositories (id, format_version, display_name, root_path, encryption_mode, created_at_utc, last_opened_at_utc)
            VALUES ($id, $format_version, $display_name, $root_path, $encryption_mode, $created_at_utc, $last_opened_at_utc);
        ";
        cmd.Parameters.AddWithValue("$id", metadata.RepositoryId.ToString());
        cmd.Parameters.AddWithValue("$format_version", metadata.FormatVersion);
        cmd.Parameters.AddWithValue("$display_name", metadata.DisplayName);
        cmd.Parameters.AddWithValue("$root_path", metadata.RootPath);
        cmd.Parameters.AddWithValue("$encryption_mode", (int)metadata.EncryptionMode);
        cmd.Parameters.AddWithValue("$created_at_utc", metadata.CreatedAtUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$last_opened_at_utc", metadata.LastOpenedAtUtc.ToString("o"));

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<RepositoryMetadata?> GetRepositoryMetadataAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, format_version, display_name, root_path, encryption_mode, created_at_utc, last_opened_at_utc FROM repositories LIMIT 1;";

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return new RepositoryMetadata
        {
            RepositoryId = Guid.Parse(reader.GetString(0)),
            FormatVersion = reader.GetString(1),
            DisplayName = reader.GetString(2),
            RootPath = reader.GetString(3),
            EncryptionMode = (EncryptionMode)reader.GetInt32(4),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(5)),
            LastOpenedAtUtc = DateTimeOffset.Parse(reader.GetString(6))
        };
    }

    public async Task UpdateLastOpenedAsync(Guid repositoryId, DateTimeOffset openedAt, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE repositories SET last_opened_at_utc = $opened_at WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", repositoryId.ToString());
        cmd.Parameters.AddWithValue("$opened_at", openedAt.ToString("o"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RegisterOrUpdateDeviceAsync(DeviceRecord device, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO devices (
                id, display_name, manufacturer, brand, model, model_code, product, codename,
                android_version, api_level, build_fingerprint, security_patch, identity_confidence,
                first_seen_utc, last_seen_utc, device_state, notes
            )
            VALUES (
                $id, $display_name, $manufacturer, $brand, $model, $model_code, $product, $codename,
                $android_version, $api_level, $build_fingerprint, $security_patch, $identity_confidence,
                $first_seen_utc, $last_seen_utc, $device_state, $notes
            )
            ON CONFLICT(id) DO UPDATE SET
                display_name = excluded.display_name,
                android_version = excluded.android_version,
                api_level = excluded.api_level,
                build_fingerprint = excluded.build_fingerprint,
                security_patch = excluded.security_patch,
                identity_confidence = excluded.identity_confidence,
                last_seen_utc = excluded.last_seen_utc,
                device_state = excluded.device_state,
                notes = excluded.notes;
        ";
        cmd.Parameters.AddWithValue("$id", device.Id.ToString());
        cmd.Parameters.AddWithValue("$display_name", device.DisplayName);
        cmd.Parameters.AddWithValue("$manufacturer", device.Manufacturer);
        cmd.Parameters.AddWithValue("$brand", device.Brand);
        cmd.Parameters.AddWithValue("$model", device.Model);
        cmd.Parameters.AddWithValue("$model_code", device.ModelCode);
        cmd.Parameters.AddWithValue("$product", device.Product);
        cmd.Parameters.AddWithValue("$codename", device.Codename);
        cmd.Parameters.AddWithValue("$android_version", device.AndroidVersion);
        cmd.Parameters.AddWithValue("$api_level", device.ApiLevel);
        cmd.Parameters.AddWithValue("$build_fingerprint", device.BuildFingerprint);
        cmd.Parameters.AddWithValue("$security_patch", device.SecurityPatch);
        cmd.Parameters.AddWithValue("$identity_confidence", device.IdentityConfidence);
        cmd.Parameters.AddWithValue("$first_seen_utc", device.FirstSeenUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$last_seen_utc", device.LastSeenUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$device_state", (int)device.State);
        cmd.Parameters.AddWithValue("$notes", (object?)device.Notes ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DeviceRecord?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, display_name, manufacturer, brand, model, model_code, product, codename, android_version, api_level, build_fingerprint, security_patch, identity_confidence, first_seen_utc, last_seen_utc, device_state, notes FROM devices WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", deviceId.ToString());

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return ReadDeviceRecord(reader);
    }

    public async Task<List<DeviceRecord>> GetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<DeviceRecord>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, display_name, manufacturer, brand, model, model_code, product, codename, android_version, api_level, build_fingerprint, security_patch, identity_confidence, first_seen_utc, last_seen_utc, device_state, notes FROM devices ORDER BY last_seen_utc DESC;";

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(ReadDeviceRecord(reader));
        }

        return list;
    }

    public async Task SaveCapabilityAssessmentAsync(CapabilityAssessment assessment, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO capability_assessments (id, device_id, assessed_at_utc, api_level, report_json)
            VALUES ($id, $device_id, $assessed_at_utc, $api_level, $report_json);
        ";
        cmd.Parameters.AddWithValue("$id", assessment.Id.ToString());
        cmd.Parameters.AddWithValue("$device_id", assessment.DeviceId.ToString());
        cmd.Parameters.AddWithValue("$assessed_at_utc", assessment.AssessedAtUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$api_level", assessment.ApiLevel);
        cmd.Parameters.AddWithValue("$report_json", JsonSerializer.Serialize(assessment.CategoryCapabilities));

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<CapabilityAssessment?> GetLatestCapabilityAssessmentAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, device_id, assessed_at_utc, api_level, report_json FROM capability_assessments WHERE device_id = $device_id ORDER BY assessed_at_utc DESC LIMIT 1;";
        cmd.Parameters.AddWithValue("$device_id", deviceId.ToString());

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var items = JsonSerializer.Deserialize<Dictionary<BackupCategory, CapabilityReportItem>>(reader.GetString(4)) ?? new();
        return new CapabilityAssessment
        {
            Id = Guid.Parse(reader.GetString(0)),
            DeviceId = Guid.Parse(reader.GetString(1)),
            AssessedAtUtc = DateTimeOffset.Parse(reader.GetString(2)),
            ApiLevel = reader.GetInt32(3),
            CategoryCapabilities = items
        };
    }

    public async Task SaveSnapshotAsync(SnapshotRecord snapshot, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();

        // 1. Insert snapshot record
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT OR REPLACE INTO snapshots (id, device_id, repository_id, parent_snapshot_id, created_at_utc, state, total_bytes, total_items, manifest_object_id, verification_status, notes)
                VALUES ($id, $device_id, $repository_id, $parent_snapshot_id, $created_at_utc, $state, $total_bytes, $total_items, $manifest_object_id, $verification_status, $notes);
            ";
            cmd.Parameters.AddWithValue("$id", snapshot.Id.ToString());
            cmd.Parameters.AddWithValue("$device_id", snapshot.DeviceId.ToString());
            cmd.Parameters.AddWithValue("$repository_id", snapshot.RepositoryId.ToString());
            cmd.Parameters.AddWithValue("$parent_snapshot_id", (object?)snapshot.ParentSnapshotId?.ToString() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$created_at_utc", snapshot.CreatedAtUtc.ToString("o"));
            cmd.Parameters.AddWithValue("$state", (int)snapshot.State);
            cmd.Parameters.AddWithValue("$total_bytes", snapshot.TotalBytes);
            cmd.Parameters.AddWithValue("$total_items", snapshot.TotalItems);
            cmd.Parameters.AddWithValue("$manifest_object_id", snapshot.ManifestObjectId);
            cmd.Parameters.AddWithValue("$verification_status", (int)snapshot.VerificationStatus);
            cmd.Parameters.AddWithValue("$notes", DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // 2. Insert items
        foreach (var item in snapshot.Items)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT OR REPLACE INTO backup_items (id, snapshot_id, category, logical_path, object_id, size_bytes, sha256, source_provider, last_modified_utc)
                VALUES ($id, $snapshot_id, $category, $logical_path, $object_id, $size_bytes, $sha256, $source_provider, $last_modified_utc);
            ";
            cmd.Parameters.AddWithValue("$id", item.Id.ToString());
            cmd.Parameters.AddWithValue("$snapshot_id", snapshot.Id.ToString());
            cmd.Parameters.AddWithValue("$category", (int)item.Category);
            cmd.Parameters.AddWithValue("$logical_path", item.LogicalPath);
            cmd.Parameters.AddWithValue("$object_id", item.ObjectId);
            cmd.Parameters.AddWithValue("$size_bytes", item.SizeBytes);
            cmd.Parameters.AddWithValue("$sha256", item.Sha256);
            cmd.Parameters.AddWithValue("$source_provider", item.SourceProvider);
            cmd.Parameters.AddWithValue("$last_modified_utc", (object?)item.LastModifiedUtc?.ToString("o") ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        tx.Commit();
    }

    public async Task<SnapshotRecord?> GetSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, device_id, repository_id, parent_snapshot_id, created_at_utc, state, total_bytes, total_items, manifest_object_id, verification_status FROM snapshots WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", snapshotId.ToString());

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var snapshot = new SnapshotRecord
        {
            Id = Guid.Parse(reader.GetString(0)),
            DeviceId = Guid.Parse(reader.GetString(1)),
            RepositoryId = Guid.Parse(reader.GetString(2)),
            ParentSnapshotId = reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(4)),
            State = (SnapshotState)reader.GetInt32(5),
            TotalBytes = reader.GetInt64(6),
            TotalItems = reader.GetInt64(7),
            ManifestObjectId = reader.GetString(8),
            VerificationStatus = (VerificationStatus)reader.GetInt32(9)
        };

        var items = await GetBackupItemsAsync(snapshot.Id, cancellationToken);
        snapshot.Items.AddRange(items);
        return snapshot;
    }

    public async Task<List<SnapshotRecord>> GetSnapshotsForDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var list = new List<SnapshotRecord>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, device_id, repository_id, parent_snapshot_id, created_at_utc, state, total_bytes, total_items, manifest_object_id, verification_status FROM snapshots WHERE device_id = $device_id ORDER BY created_at_utc DESC;";
        cmd.Parameters.AddWithValue("$device_id", deviceId.ToString());

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new SnapshotRecord
            {
                Id = Guid.Parse(reader.GetString(0)),
                DeviceId = Guid.Parse(reader.GetString(1)),
                RepositoryId = Guid.Parse(reader.GetString(2)),
                ParentSnapshotId = reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
                CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(4)),
                State = (SnapshotState)reader.GetInt32(5),
                TotalBytes = reader.GetInt64(6),
                TotalItems = reader.GetInt64(7),
                ManifestObjectId = reader.GetString(8),
                VerificationStatus = (VerificationStatus)reader.GetInt32(9)
            });
        }

        return list;
    }

    public async Task<List<BackupItemRecord>> GetBackupItemsAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        var items = new List<BackupItemRecord>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, snapshot_id, category, logical_path, object_id, size_bytes, sha256, source_provider, last_modified_utc FROM backup_items WHERE snapshot_id = $snapshot_id;";
        cmd.Parameters.AddWithValue("$snapshot_id", snapshotId.ToString());

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new BackupItemRecord
            {
                Id = Guid.Parse(reader.GetString(0)),
                SnapshotId = Guid.Parse(reader.GetString(1)),
                Category = (BackupCategory)reader.GetInt32(2),
                LogicalPath = reader.GetString(3),
                ObjectId = reader.GetString(4),
                SizeBytes = reader.GetInt64(5),
                Sha256 = reader.GetString(6),
                SourceProvider = reader.GetString(7),
                LastModifiedUtc = reader.IsDBNull(8) ? null : DateTimeOffset.Parse(reader.GetString(8))
            });
        }

        return items;
    }

    public async Task UpdateSnapshotVerificationAsync(Guid snapshotId, VerificationStatus status, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE snapshots SET verification_status = $status WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", snapshotId.ToString());
        cmd.Parameters.AddWithValue("$status", (int)status);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RecordObjectReferenceAsync(string objectId, long sizeBytes, EncryptionMode mode, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO objects (object_id, size_bytes, encryption_mode, created_at_utc, reference_count, verification_status)
            VALUES ($object_id, $size_bytes, $encryption_mode, datetime('now'), 1, 1)
            ON CONFLICT(object_id) DO UPDATE SET reference_count = reference_count + 1;
        ";
        cmd.Parameters.AddWithValue("$object_id", objectId);
        cmd.Parameters.AddWithValue("$size_bytes", sizeBytes);
        cmd.Parameters.AddWithValue("$encryption_mode", (int)mode);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<List<string>> GetAllTrackedObjectIdsAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<string>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT object_id FROM objects;";

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    private static DeviceRecord ReadDeviceRecord(SqliteDataReader reader)
    {
        return new DeviceRecord
        {
            Id = Guid.Parse(reader.GetString(0)),
            DisplayName = reader.GetString(1),
            Manufacturer = reader.GetString(2),
            Brand = reader.GetString(3),
            Model = reader.GetString(4),
            ModelCode = reader.GetString(5),
            Product = reader.GetString(6),
            Codename = reader.GetString(7),
            AndroidVersion = reader.GetString(8),
            ApiLevel = reader.GetInt32(9),
            BuildFingerprint = reader.GetString(10),
            SecurityPatch = reader.GetString(11),
            IdentityConfidence = reader.GetString(12),
            FirstSeenUtc = DateTimeOffset.Parse(reader.GetString(13)),
            LastSeenUtc = DateTimeOffset.Parse(reader.GetString(14)),
            State = (DeviceState)reader.GetInt32(15),
            Notes = reader.IsDBNull(16) ? null : reader.GetString(16)
        };
    }
}
