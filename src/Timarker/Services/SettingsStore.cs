using System.Text.Json;
using Microsoft.Win32;
using Timarker.Models;

namespace Timarker.Services;

public sealed class SettingsStore
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "Timarker";
    private const string LegacyRunValueName = "Timeline";
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsStore()
    {
        var dir = AppPaths.DataDirectory;
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            var startWithWindows = IsStartWithWindowsEnabled();
            if (startWithWindows) SetStartWithWindows(true);
            return new AppSettings { StartWithWindows = startWithWindows };
        }

        var json = File.ReadAllText(_filePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        settings.StartWithWindows = IsStartWithWindowsEnabled();
        if (settings.StartWithWindows) SetStartWithWindows(true);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        SetStartWithWindows(settings.StartWithWindows);
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static bool IsStartWithWindowsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return !string.IsNullOrWhiteSpace(key?.GetValue(RunValueName)?.ToString())
            || !string.IsNullOrWhiteSpace(key?.GetValue(LegacyRunValueName)?.ToString());
    }

    private static void SetStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            key.SetValue(RunValueName, $"\"{Application.ExecutablePath}\"");
            key.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
        }
        else
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
            key.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
        }
    }
}
