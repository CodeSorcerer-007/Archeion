using Microsoft.Data.Sqlite;

namespace Archeion.Persistence;

public sealed class DatabaseMigrator
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public DatabaseMigrator(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void MigrateToLatest()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        // 1. Ensure migrations table exists
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS repository_migrations (
                    version INTEGER PRIMARY KEY,
                    applied_at_utc TEXT NOT NULL,
                    description TEXT NOT NULL
                );
            ";
            cmd.ExecuteNonQuery();
        }

        var currentVersion = GetCurrentVersion(connection, transaction);

        if (currentVersion < 1)
        {
            ApplyMigration1(connection, transaction);
        }

        transaction.Commit();
    }

    private static int GetCurrentVersion(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM repository_migrations;";
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    private static void ApplyMigration1(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
            -- Repositories table
            CREATE TABLE IF NOT EXISTS repositories (
                id TEXT PRIMARY KEY,
                format_version TEXT NOT NULL,
                display_name TEXT NOT NULL,
                root_path TEXT NOT NULL,
                encryption_mode INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                last_opened_at_utc TEXT NOT NULL
            );

            -- Devices table
            CREATE TABLE IF NOT EXISTS devices (
                id TEXT PRIMARY KEY,
                display_name TEXT NOT NULL,
                manufacturer TEXT NOT NULL,
                brand TEXT NOT NULL,
                model TEXT NOT NULL,
                model_code TEXT NOT NULL,
                product TEXT NOT NULL,
                codename TEXT NOT NULL,
                android_version TEXT NOT NULL,
                api_level INTEGER NOT NULL,
                build_fingerprint TEXT NOT NULL,
                security_patch TEXT NOT NULL,
                identity_confidence TEXT NOT NULL,
                first_seen_utc TEXT NOT NULL,
                last_seen_utc TEXT NOT NULL,
                device_state INTEGER NOT NULL,
                notes TEXT
            );

            -- Capability assessments
            CREATE TABLE IF NOT EXISTS capability_assessments (
                id TEXT PRIMARY KEY,
                device_id TEXT NOT NULL,
                assessed_at_utc TEXT NOT NULL,
                api_level INTEGER NOT NULL,
                report_json TEXT NOT NULL,
                FOREIGN KEY (device_id) REFERENCES devices(id) ON DELETE CASCADE
            );

            -- Backup plans
            CREATE TABLE IF NOT EXISTS backup_plans (
                id TEXT PRIMARY KEY,
                device_id TEXT NOT NULL,
                repository_id TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                plan_json TEXT NOT NULL,
                FOREIGN KEY (device_id) REFERENCES devices(id) ON DELETE CASCADE,
                FOREIGN KEY (repository_id) REFERENCES repositories(id) ON DELETE CASCADE
            );

            -- Snapshots table
            CREATE TABLE IF NOT EXISTS snapshots (
                id TEXT PRIMARY KEY,
                device_id TEXT NOT NULL,
                repository_id TEXT NOT NULL,
                parent_snapshot_id TEXT,
                created_at_utc TEXT NOT NULL,
                state INTEGER NOT NULL,
                total_bytes INTEGER NOT NULL,
                total_items INTEGER NOT NULL,
                manifest_object_id TEXT NOT NULL,
                verification_status INTEGER NOT NULL,
                notes TEXT,
                FOREIGN KEY (device_id) REFERENCES devices(id) ON DELETE CASCADE,
                FOREIGN KEY (repository_id) REFERENCES repositories(id) ON DELETE CASCADE,
                FOREIGN KEY (parent_snapshot_id) REFERENCES snapshots(id) ON DELETE SET NULL
            );

            -- Backup items table
            CREATE TABLE IF NOT EXISTS backup_items (
                id TEXT PRIMARY KEY,
                snapshot_id TEXT NOT NULL,
                category INTEGER NOT NULL,
                logical_path TEXT NOT NULL,
                object_id TEXT NOT NULL,
                size_bytes INTEGER NOT NULL,
                sha256 TEXT NOT NULL,
                source_provider TEXT NOT NULL,
                last_modified_utc TEXT,
                FOREIGN KEY (snapshot_id) REFERENCES snapshots(id) ON DELETE CASCADE
            );

            -- Objects reference table
            CREATE TABLE IF NOT EXISTS objects (
                object_id TEXT PRIMARY KEY,
                size_bytes INTEGER NOT NULL,
                encryption_mode INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                reference_count INTEGER NOT NULL,
                verification_status INTEGER NOT NULL
            );

            -- Indexes for fast querying and exploration
            CREATE INDEX IF NOT EXISTS idx_snapshots_device_id ON snapshots(device_id);
            CREATE INDEX IF NOT EXISTS idx_backup_items_snapshot_id ON backup_items(snapshot_id);
            CREATE INDEX IF NOT EXISTS idx_backup_items_category ON backup_items(category);
            CREATE INDEX IF NOT EXISTS idx_backup_items_sha256 ON backup_items(sha256);
            CREATE INDEX IF NOT EXISTS idx_objects_refcount ON objects(reference_count);

            -- Record migration
            INSERT INTO repository_migrations (version, applied_at_utc, description)
            VALUES (1, datetime('now'), 'Baseline schema for ARCHEION repository');
        ";
        cmd.ExecuteNonQuery();
    }
}
