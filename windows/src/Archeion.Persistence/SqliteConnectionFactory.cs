using Microsoft.Data.Sqlite;

namespace Archeion.Persistence;

public interface ISqliteConnectionFactory
{
    SqliteConnection CreateConnection();
}

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string databasePath)
    {
        ArgumentNullException.ThrowIfNull(databasePath);
        var dir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        _connectionString = builder.ToString();
    }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            PRAGMA foreign_keys = ON;
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
        ";
        cmd.ExecuteNonQuery();

        return connection;
    }
}
