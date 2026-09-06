using Microsoft.Data.Sqlite;
using System.IO;

namespace NorthstarBrowser.Services;

public sealed class NewtonDataStore : IDisposable
{
    public const int CurrentSchemaVersion = 3;
    private readonly SqliteConnection _connection;
    private readonly string _databasePath;
    public bool WasPreviousShutdownClean { get; private set; } = true;

    public NewtonDataStore()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Newton", "Data");
        Directory.CreateDirectory(directory);
        _databasePath = Path.Combine(directory, "newton.db");
        _connection = new SqliteConnection($"Data Source={_databasePath};Mode=ReadWriteCreate");
    }

    public void Initialise()
    {
        _connection.Open();
        Execute("PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL;");
        var version = Convert.ToInt32(Scalar("PRAGMA user_version;") ?? 0);
        if (version > CurrentSchemaVersion)
            throw new InvalidOperationException($"Newton data schema {version} is newer than this application supports.");

        if (version > 0 && version < CurrentSchemaVersion) CreatePreMigrationBackup(version);

        if (version < 1)
        {
            using var transaction = _connection.BeginTransaction();
            Execute("""
                CREATE TABLE app_state(key TEXT PRIMARY KEY, value TEXT NOT NULL);
                CREATE TABLE history(id INTEGER PRIMARY KEY, url TEXT NOT NULL, title TEXT NOT NULL DEFAULT '', visited_utc TEXT NOT NULL);
                CREATE INDEX ix_history_visited ON history(visited_utc DESC);
                CREATE TABLE bookmarks(id INTEGER PRIMARY KEY, url TEXT NOT NULL UNIQUE, title TEXT NOT NULL, created_utc TEXT NOT NULL);
                CREATE TABLE recovery_tabs(id INTEGER PRIMARY KEY, workspace_position INTEGER NOT NULL, workspace_name TEXT NOT NULL, tab_position INTEGER NOT NULL, url TEXT NOT NULL, title TEXT NOT NULL, group_name TEXT NOT NULL);
                PRAGMA user_version=1;
                """, transaction);
            transaction.Commit();
            version = 1;
        }

        if (version < 2)
        {
            using var transaction = _connection.BeginTransaction();
            Execute("""
                CREATE TABLE profiles(id TEXT PRIMARY KEY, name TEXT NOT NULL, is_private INTEGER NOT NULL DEFAULT 0, created_utc TEXT NOT NULL);
                CREATE TABLE workspaces(id INTEGER PRIMARY KEY, profile_id TEXT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE, name TEXT NOT NULL, position INTEGER NOT NULL);
                ALTER TABLE recovery_tabs ADD COLUMN profile_id TEXT NOT NULL DEFAULT 'default';
                INSERT OR IGNORE INTO profiles(id,name,is_private,created_utc) VALUES('default','Default',0,datetime('now'));
                PRAGMA user_version=2;
                """, transaction);
            transaction.Commit();
            version = 2;
        }

        if (version < 3)
        {
            using var transaction = _connection.BeginTransaction();
            Execute("""
                ALTER TABLE recovery_tabs ADD COLUMN snapshot_version INTEGER NOT NULL DEFAULT 1;
                ALTER TABLE recovery_tabs ADD COLUMN workspace_id TEXT NOT NULL DEFAULT '';
                ALTER TABLE recovery_tabs ADD COLUMN tab_id TEXT NOT NULL DEFAULT '';
                PRAGMA user_version=3;
                """, transaction);
            transaction.Commit();
        }

        var clean = Scalar("SELECT value FROM app_state WHERE key='clean_shutdown';")?.ToString();
        WasPreviousShutdownClean = clean is null or "1";
        SetState("clean_shutdown", "0");
    }

    private void CreatePreMigrationBackup(int sourceVersion)
    {
        var backupPath = $"{_databasePath}.pre-migration-v{sourceVersion}.bak";
        using var backup = new SqliteConnection($"Data Source={backupPath};Mode=ReadWriteCreate");
        backup.Open();
        _connection.BackupDatabase(backup);
    }

    public IReadOnlyList<RecoveryTab> LoadRecoveryTabs()
    {
        var result = new List<RecoveryTab>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT workspace_position, workspace_name, tab_position, url, title, group_name, snapshot_version, workspace_id, tab_id FROM recovery_tabs ORDER BY workspace_position, tab_position;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var url = reader.IsDBNull(3) ? null : reader.GetString(3);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https" or "file")) continue;
            var group = reader.IsDBNull(5) ? "General" : reader.GetString(5);
            if (string.IsNullOrWhiteSpace(group)) group = "General";
            result.Add(new RecoveryTab(
                Math.Max(0, reader.GetInt32(0)),
                reader.IsDBNull(1) || string.IsNullOrWhiteSpace(reader.GetString(1)) ? "Recovered" : reader.GetString(1),
                Math.Max(0, reader.GetInt32(2)), parsed.AbsoluteUri,
                reader.IsDBNull(4) ? "Recovered page" : reader.GetString(4), group,
                reader.GetInt32(6), reader.GetString(7), reader.GetString(8)));
        }
        return result;
    }

    public void SaveRecoverySnapshot(IEnumerable<RecoveryTab> tabs, bool cleanShutdown)
    {
        using var transaction = _connection.BeginTransaction();
        Execute("DELETE FROM recovery_tabs;", transaction);
        foreach (var tab in tabs)
        {
            using var command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO recovery_tabs(workspace_position,workspace_name,tab_position,url,title,group_name,snapshot_version,workspace_id,tab_id) VALUES($wp,$wn,$tp,$url,$title,$group,2,$wid,$tid);";
            command.Parameters.AddWithValue("$wp", tab.WorkspacePosition);
            command.Parameters.AddWithValue("$wn", tab.WorkspaceName);
            command.Parameters.AddWithValue("$tp", tab.TabPosition);
            command.Parameters.AddWithValue("$url", tab.Url);
            command.Parameters.AddWithValue("$title", tab.Title);
            command.Parameters.AddWithValue("$group", tab.Group);
            command.Parameters.AddWithValue("$wid", tab.WorkspaceId);
            command.Parameters.AddWithValue("$tid", tab.TabId);
            command.ExecuteNonQuery();
        }
        SetState("clean_shutdown", cleanShutdown ? "1" : "0", transaction);
        transaction.Commit();
    }

    private void SetState(string key, string value, SqliteTransaction? transaction = null)
    {
        using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO app_state(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=excluded.value;";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private object? Scalar(string sql) { using var c = _connection.CreateCommand(); c.CommandText = sql; return c.ExecuteScalar(); }
    private void Execute(string sql, SqliteTransaction? transaction = null) { using var c = _connection.CreateCommand(); c.Transaction = transaction; c.CommandText = sql; c.ExecuteNonQuery(); }
    public void Dispose() => _connection.Dispose();
}

public sealed record RecoveryTab(
    int WorkspacePosition, string WorkspaceName, int TabPosition, string Url, string Title, string Group,
    int SnapshotVersion = 2, string WorkspaceId = "", string TabId = "");
