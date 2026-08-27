using Timarker.Models;
using Timarker.Services;
using System.Text.Json;

namespace Timarker_WinUI.Services;

public sealed class AppData
{
    public static AppData Current { get; } = new();
    private readonly EventStore _store;
    private readonly string _settingsPath;

    public List<EventItem> Events { get; }
    public List<Folder> Folders => _store.Folders;
    public List<Project> Projects => _store.Projects;
    public List<ActivityRecord> Records => _store.Records;
    public List<NoteItem> Notes => _store.Notes;
    public AppSettings Settings { get; private set; }

    private AppData()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Timarker");
        _store = new EventStore(directory);
        Events = _store.Load();
        _settingsPath = Path.Combine(directory, "settings.json");
        Settings = File.Exists(_settingsPath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath)) ?? new() : new();
    }

    public void Save() => _store.Save(Events, records: Records, notes: Notes, folders: Folders, projects: Projects);
    public void SaveSettings() => File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true }));
}
