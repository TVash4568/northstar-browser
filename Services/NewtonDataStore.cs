using Microsoft.Data.Sqlite;
using System.IO;

namespace NorthstarBrowser.Services;

public sealed class NewtonDataStore : IDisposable
{
    public const int CurrentSchemaVersion = 2;
    private readonly SqliteConnection _connection;
    public bool WasPreviousShutdownClean { get; private set; } = true;

    public NewtonDataStore()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Newton", "Data");
        Directory.CreateDirectory(directory);
        _connection = new SqliteConnection($"Data Source={Path.Combine(directory, "newton.db")};Mode=ReadWriteCreate");
    }

    public void Initialise()
    {
        _connection.Open();
        Execute("PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL;");
        var version = Convert.ToInt32(Scalar("PRAGMA user_version;") ?? 0);
        if (version > CurrentSchemaVersion)
            throw new InvalidOperationException($"Newton data schema {version} is newer than this application supports.");

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
        }

        var clean = Scalar("SELECT value FROM app_state WHERE key='clean_shutdown';")?.ToString();
        WasPreviousShutdownClean = clean is null or "1";
        SetState("clean_shutdown", "0");
    }

    public IReadOnlyList<RecoveryTab> LoadRecoveryTabs()
    {
        var result = new List<RecoveryTab>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT workspace_position, workspace_name, tab_position, url, title, group_name FROM recovery_tabs ORDER BY workspace_position, tab_position;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            result.Add(new RecoveryTab(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3), reader.GetString(4), reader.GetString(5)));
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
            command.CommandText = "INSERT INTO recovery_tabs(workspace_position,workspace_name,tab_position,url,title,group_name) VALUES($wp,$wn,$tp,$url,$title,$group);";
            command.Parameters.AddWithValue("$wp", tab.WorkspacePosition);
            command.Parameters.AddWithValue("$wn", tab.WorkspaceName);
            command.Parameters.AddWithValue("$tp", tab.TabPosition);
            command.Parameters.AddWithValue("$url", tab.Url);
            command.Parameters.AddWithValue("$title", tab.Title);
            command.Parameters.AddWithValue("$group", tab.Group);
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

public sealed record RecoveryTab(int WorkspacePosition, string WorkspaceName, int TabPosition, string Url, string Title, string Group);
