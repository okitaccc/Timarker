using System.Text.Json;
using Microsoft.Data.Sqlite;
using Timarker.Models;

namespace Timarker.Services;

public sealed class ActivityStore
{
    private const int SchemaVersion = 1;
    private const int BackupLimit = 7;
    private readonly object _gate = new();
    private readonly string _databasePath;
    private readonly string _legacyPath;
    private readonly string _backupDirectory;
    private HashSet<Guid> _savedSessionIds = [];
    private HashSet<Guid> _savedRuleIds = [];

    public ActivityStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _databasePath = Path.Combine(directory, "activity.db");
        _legacyPath = Path.Combine(directory, "activity.json");
        _backupDirectory = Path.Combine(directory, "ActivityBackups");
        try
        {
            Initialize();
            ValidateIntegrity();
            Load();
            ImportLegacyJson();
        }
        catch (Exception ex) when (ex is SqliteException or InvalidDataException or FormatException)
        {
            QuarantineDatabase();
            var restoredBackup = RestoreLatestBackup();
            Initialize();
            ValidateIntegrity();
            Load();
            RecoveryMessage = restoredBackup is null
                ? "时间追踪数据库无法读取，已保留损坏文件并创建新的数据库。事项、项目和便签数据不受影响。"
                : $"时间追踪数据库无法读取，已从备份 {restoredBackup} 恢复。损坏文件仍保留在 Corrupt 目录中。";
        }
    }

    public List<AppActivitySession> Sessions { get; private set; } = [];
    public List<AppActivityRule> Rules { get; private set; } = [];
    public string? RecoveryMessage { get; private set; }

    public void Load()
    {
        lock (_gate)
        {
            Sessions = [];
            Rules = [];
            using var connection = Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, started_at, ended_at, process_name, app_name, window_title, category, is_idle, project_id, event_id FROM activity_sessions ORDER BY started_at";
                using var reader = command.ExecuteReader();
                while (reader.Read()) Sessions.Add(new AppActivitySession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartedAt = new DateTime(reader.GetInt64(1), DateTimeKind.Local),
                    EndedAt = new DateTime(reader.GetInt64(2), DateTimeKind.Local),
                    ProcessName = reader.GetString(3),
                    AppName = reader.GetString(4),
                    WindowTitle = reader.GetString(5),
                    Category = reader.GetString(6),
                    IsIdle = reader.GetBoolean(7),
                    ProjectId = GuidValue(reader, 8),
                    EventId = GuidValue(reader, 9)
                });
            }
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, process_name, title_contains, category, project_id, event_id FROM activity_rules";
                using var reader = command.ExecuteReader();
                while (reader.Read()) Rules.Add(new AppActivityRule
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    ProcessName = reader.GetString(1),
                    TitleContains = reader.GetString(2),
                    Category = reader.GetString(3),
                    ProjectId = GuidValue(reader, 4),
                    EventId = GuidValue(reader, 5)
                });
            }
            _savedSessionIds = Sessions.Select(x => x.Id).ToHashSet();
            _savedRuleIds = Rules.Select(x => x.Id).ToHashSet();
        }
    }

    public void Save(int retentionDays, bool saveAllSessions = false)
    {
        lock (_gate)
        {
            var oldest = DateTime.Now.Date.AddDays(-Math.Max(7, retentionDays));
            Sessions.RemoveAll(x => x.EndedAt < oldest);
            using var connection = Open();
            using var transaction = connection.BeginTransaction();
            SaveSessions(connection, transaction, saveAllSessions);
            SaveRules(connection, transaction);
            DeleteMissing(connection, transaction, "activity_sessions", _savedSessionIds, Sessions.Select(x => x.Id));
            DeleteMissing(connection, transaction, "activity_rules", _savedRuleIds, Rules.Select(x => x.Id));
            transaction.Commit();
            _savedSessionIds = Sessions.Select(x => x.Id).ToHashSet();
            _savedRuleIds = Rules.Select(x => x.Id).ToHashSet();
            CreateDailyBackup(connection);
            RunScheduledMaintenance(connection, retentionDays);
        }
    }

    public void ClearSessions()
    {
        lock (_gate)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM activity_sessions";
            command.ExecuteNonQuery();
            Sessions.Clear();
            _savedSessionIds.Clear();
        }
    }

    public void Maintain(int retentionDays, bool compact = true)
    {
        lock (_gate)
        {
            var oldestTicks = DateTime.Now.Date.AddDays(-Math.Max(7, retentionDays)).Ticks;
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM activity_sessions WHERE ended_at < $oldest; PRAGMA optimize;";
            command.Parameters.AddWithValue("$oldest", oldestTicks);
            command.ExecuteNonQuery();
            if (compact)
            {
                using var vacuum = connection.CreateCommand();
                vacuum.CommandText = "VACUUM";
                vacuum.ExecuteNonQuery();
            }
            Sessions.RemoveAll(x => x.EndedAt.Ticks < oldestTicks);
            _savedSessionIds = Sessions.Select(x => x.Id).ToHashSet();
            SetMeta(connection, "last_maintenance", DateTime.UtcNow.Ticks.ToString());
        }
    }

    public AppActivityRule? MatchRule(string processName, string title) =>
        Rules.Where(x => x.Matches(processName, title)).OrderByDescending(x => x.TitleContains.Length).FirstOrDefault();

    private void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS activity_sessions (
                id TEXT PRIMARY KEY, started_at INTEGER NOT NULL, ended_at INTEGER NOT NULL,
                process_name TEXT NOT NULL, app_name TEXT NOT NULL, window_title TEXT NOT NULL,
                category TEXT NOT NULL, is_idle INTEGER NOT NULL, project_id TEXT, event_id TEXT);
            CREATE INDEX IF NOT EXISTS ix_activity_sessions_time ON activity_sessions(started_at, ended_at);
            CREATE TABLE IF NOT EXISTS activity_rules (
                id TEXT PRIMARY KEY, process_name TEXT NOT NULL, title_contains TEXT NOT NULL,
                category TEXT NOT NULL, project_id TEXT, event_id TEXT);
            CREATE TABLE IF NOT EXISTS activity_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();
        using var version = connection.CreateCommand();
        version.CommandText = "PRAGMA user_version";
        if (Convert.ToInt32(version.ExecuteScalar()) == 0)
        {
            version.CommandText = $"PRAGMA user_version={SchemaVersion}";
            version.ExecuteNonQuery();
        }
    }

    private void ValidateIntegrity()
    {
        using var connection = Open();
        using var version = connection.CreateCommand();
        version.CommandText = "PRAGMA user_version";
        var currentVersion = Convert.ToInt32(version.ExecuteScalar());
        if (currentVersion > SchemaVersion) throw new InvalidDataException("时间追踪数据库来自更高版本的 Timarker。");
        using var integrity = connection.CreateCommand();
        integrity.CommandText = "PRAGMA quick_check";
        if (!string.Equals(integrity.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("时间追踪数据库完整性检查失败。");
    }

    private void CreateDailyBackup(SqliteConnection source)
    {
        Directory.CreateDirectory(_backupDirectory);
        var path = Path.Combine(_backupDirectory, $"activity-{DateTime.Today:yyyyMMdd}.db");
        if (!File.Exists(path))
        {
            using var destination = new SqliteConnection($"Data Source={path};Mode=ReadWriteCreate");
            destination.Open();
            source.BackupDatabase(destination);
        }
        foreach (var old in Directory.GetFiles(_backupDirectory, "activity-*.db").OrderByDescending(File.GetLastWriteTimeUtc).Skip(BackupLimit))
            File.Delete(old);
    }

    private void QuarantineDatabase()
    {
        SqliteConnection.ClearAllPools();
        var directory = Path.Combine(Path.GetDirectoryName(_databasePath)!, "Corrupt");
        Directory.CreateDirectory(directory);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var source = _databasePath + suffix;
            if (File.Exists(source)) File.Move(source, Path.Combine(directory, $"activity-{stamp}.db{suffix}"), true);
        }
    }

    private string? RestoreLatestBackup()
    {
        if (!Directory.Exists(_backupDirectory)) return null;
        foreach (var backup in Directory.GetFiles(_backupDirectory, "activity-*.db").OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={backup};Mode=ReadOnly");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA quick_check";
                if (!string.Equals(command.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase)) continue;
                connection.Close();
                File.Copy(backup, _databasePath, true);
                return Path.GetFileName(backup);
            }
            catch (SqliteException) { }
            catch (IOException) { }
        }
        return null;
    }

    private void ImportLegacyJson()
    {
        if (!File.Exists(_legacyPath) || Sessions.Count != 0 || Rules.Count != 0) return;
        try
        {
            using var stream = File.OpenRead(_legacyPath);
            var data = JsonSerializer.Deserialize<ActivityDocument>(stream);
            Sessions = data?.Sessions ?? [];
            Rules = data?.Rules ?? [];
            Save(365);
            File.Move(_legacyPath, _legacyPath + ".migrated", true);
        }
        catch (Exception ex) when (ex is JsonException or IOException or SqliteException) { }
    }

    private void SaveSessions(SqliteConnection connection, SqliteTransaction transaction, bool saveAll)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO activity_sessions VALUES ($id,$start,$end,$process,$app,$title,$category,$idle,$project,$event)
            ON CONFLICT(id) DO UPDATE SET started_at=$start,ended_at=$end,process_name=$process,app_name=$app,
            window_title=$title,category=$category,is_idle=$idle,project_id=$project,event_id=$event
            """;
        // The collector only changes the newest session; rules can request a full reclassification save.
        var changed = saveAll ? Sessions : Sessions.Where(x => !_savedSessionIds.Contains(x.Id)).Concat(Sessions.TakeLast(1)).DistinctBy(x => x.Id);
        foreach (var item in changed)
        {
            command.Parameters.Clear();
            command.Parameters.AddWithValue("$id", item.Id.ToString());
            command.Parameters.AddWithValue("$start", item.StartedAt.Ticks);
            command.Parameters.AddWithValue("$end", item.EndedAt.Ticks);
            command.Parameters.AddWithValue("$process", item.ProcessName);
            command.Parameters.AddWithValue("$app", item.AppName);
            command.Parameters.AddWithValue("$title", item.WindowTitle);
            command.Parameters.AddWithValue("$category", item.Category);
            command.Parameters.AddWithValue("$idle", item.IsIdle);
            command.Parameters.AddWithValue("$project", (object?)item.ProjectId?.ToString() ?? DBNull.Value);
            command.Parameters.AddWithValue("$event", (object?)item.EventId?.ToString() ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private void SaveRules(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO activity_rules VALUES ($id,$process,$title,$category,$project,$event)
            ON CONFLICT(id) DO UPDATE SET process_name=$process,title_contains=$title,category=$category,project_id=$project,event_id=$event
            """;
        foreach (var item in Rules)
        {
            command.Parameters.Clear();
            command.Parameters.AddWithValue("$id", item.Id.ToString());
            command.Parameters.AddWithValue("$process", item.ProcessName);
            command.Parameters.AddWithValue("$title", item.TitleContains);
            command.Parameters.AddWithValue("$category", item.Category);
            command.Parameters.AddWithValue("$project", (object?)item.ProjectId?.ToString() ?? DBNull.Value);
            command.Parameters.AddWithValue("$event", (object?)item.EventId?.ToString() ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void DeleteMissing(SqliteConnection connection, SqliteTransaction transaction, string table, HashSet<Guid> saved, IEnumerable<Guid> current)
    {
        var removed = saved.Except(current).ToList();
        if (removed.Count == 0) return;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM {table} WHERE id=$id";
        var id = command.Parameters.Add("$id", SqliteType.Text);
        foreach (var value in removed) { id.Value = value.ToString(); command.ExecuteNonQuery(); }
    }

    private void RunScheduledMaintenance(SqliteConnection connection, int retentionDays)
    {
        var text = GetMeta(connection, "last_maintenance");
        if (long.TryParse(text, out var ticks) && DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) < TimeSpan.FromDays(7)) return;
        Maintain(retentionDays, false);
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath};Mode=ReadWriteCreate;Cache=Shared");
        connection.Open();
        return connection;
    }

    private static Guid? GuidValue(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));
    private static string? GetMeta(SqliteConnection connection, string key)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM activity_meta WHERE key=$key";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar()?.ToString();
    }
    private static void SetMeta(SqliteConnection connection, string key, string value)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO activity_meta(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=$value";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private sealed class ActivityDocument
    {
        public List<AppActivitySession> Sessions { get; set; } = [];
        public List<AppActivityRule> Rules { get; set; } = [];
    }
}
