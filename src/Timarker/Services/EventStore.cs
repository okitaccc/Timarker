using System.Text.Json;
using Timarker.Models;

namespace Timarker.Services;

public sealed class EventStore
{
    private const int BackupLimit = 10;
    private readonly object _gate = new();
    private readonly string _filePath;
    private readonly string _fallbackPath;
    private readonly string _backupDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public string? RecoveryMessage { get; private set; }
    public List<PersonProfile> People { get; private set; } = [];
    public List<ActivityRecord> Records { get; private set; } = [];
    public List<NoteItem> Notes { get; private set; } = [];

    public EventStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "events.json");
        _fallbackPath = Path.Combine(directory, "events.bak");
        _backupDirectory = Path.Combine(directory, "Backups");
    }

    public List<EventItem> Load()
    {
        RecoveryMessage = null;
        var foundDataFile = false;
        var candidates = new[] { _filePath, _fallbackPath }
            .Concat(Directory.Exists(_backupDirectory)
                ? Directory.GetFiles(_backupDirectory, "events-*.json").OrderByDescending(File.GetLastWriteTimeUtc)
                : []);

        foreach (var candidate in candidates.Where(File.Exists))
        {
            foundDataFile = true;
            try
            {
                var document = Read(candidate);
                foreach (var item in document.Events) item.NormalizeAfterLoad();
                foreach (var person in document.People) person.NormalizeAfterLoad();
                foreach (var record in document.Records) record.NormalizeAfterLoad();
                foreach (var note in document.Notes) note.NormalizeAfterLoad();
                if (document.SchemaVersion < 4) MigrateOccasionPeople(document);
                People = document.People;
                Records = document.Records;
                Notes = document.Notes;
                if (!string.Equals(candidate, _filePath, StringComparison.OrdinalIgnoreCase))
                {
                    RecoveryMessage = $"主数据文件无法读取，已从备份 {Path.GetFileName(candidate)} 恢复。";
                }
                return document.Events;
            }
            catch (JsonException)
            {
                // 继续尝试下一份备份。
            }
            catch (IOException)
            {
                // 文件可能正被系统短暂占用，继续尝试备份。
            }
        }

        if (foundDataFile)
        {
            RecoveryMessage = "数据文件无法读取，并且没有找到可用备份。原文件仍保留在数据目录中。";
        }
        return [];
    }

    public void Save(IEnumerable<EventItem> events, IEnumerable<PersonProfile>? people = null, IEnumerable<ActivityRecord>? records = null, IEnumerable<NoteItem>? notes = null)
    {
        lock (_gate)
        {
            var document = new StoreDocument
            {
                Events = events.ToList(),
                People = (people ?? People).ToList(),
                Records = (records ?? Records).ToList(),
                Notes = (notes ?? Notes).ToList()
            };
            var tempPath = _filePath + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    JsonSerializer.Serialize(stream, document, _jsonOptions);
                    stream.Flush(true);
                }

                _ = Read(tempPath); // 写入成功不等于内容完整，替换前再读一次。
                if (File.Exists(_filePath)) File.Replace(tempPath, _filePath, _fallbackPath, true);
                else File.Move(tempPath, _filePath);
                CreateRollingBackup();
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }
    }

    private StoreDocument Read(string path)
    {
        using var stream = File.OpenRead(path);
        using var json = JsonDocument.Parse(stream);
        return json.RootElement.ValueKind is JsonValueKind.Array
            ? new StoreDocument { SchemaVersion = 1, Events = json.RootElement.Deserialize<List<EventItem>>(_jsonOptions) ?? [] }
            : json.RootElement.Deserialize<StoreDocument>(_jsonOptions) ?? new StoreDocument();
    }

    private static void MigrateOccasionPeople(StoreDocument document)
    {
        foreach (var item in document.Events.Where(x => (x.Type is EventType.Birthday or EventType.Anniversary) && !string.IsNullOrWhiteSpace(x.SubjectName)))
        {
            var person = document.People.FirstOrDefault(x => x.Name.Equals(item.SubjectName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (person is null)
            {
                person = new PersonProfile { Name = item.SubjectName.Trim(), Relationship = item.Relationship.Trim() };
                document.People.Add(person);
            }
            if (!item.PersonIds.Contains(person.Id)) item.PersonIds.Add(person.Id);
        }
    }

    private void CreateRollingBackup()
    {
        Directory.CreateDirectory(_backupDirectory);
        var path = Path.Combine(_backupDirectory, $"events-{DateTime.Now:yyyyMMdd-HHmmssfffffff}.json");
        File.Copy(_filePath, path);
        foreach (var oldBackup in Directory.GetFiles(_backupDirectory, "events-*.json")
                     .OrderByDescending(File.GetLastWriteTimeUtc)
                     .Skip(BackupLimit))
        {
            File.Delete(oldBackup);
        }
    }

    private sealed class StoreDocument
    {
        public int SchemaVersion { get; set; } = 5;
        public DateTime SavedAt { get; set; } = DateTime.Now;
        public List<EventItem> Events { get; set; } = [];
        public List<PersonProfile> People { get; set; } = [];
        public List<ActivityRecord> Records { get; set; } = [];
        public List<NoteItem> Notes { get; set; } = [];
    }
}
