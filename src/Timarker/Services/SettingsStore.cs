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
    private readonly string _backupPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsStore()
    {
        var dir = AppPaths.DataDirectory;
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        _backupPath = Path.Combine(dir, "settings.bak");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            var startWithWindows = IsStartWithWindowsEnabled();
            if (startWithWindows) SetStartWithWindows(true);
            return new AppSettings { StartWithWindows = startWithWindows };
        }

        var settings = Read(_filePath);
        if (settings is null && File.Exists(_filePath))
        {
            var corruptDirectory = Path.Combine(Path.GetDirectoryName(_filePath)!, "Corrupt");
            Directory.CreateDirectory(corruptDirectory);
            File.Move(_filePath, Path.Combine(corruptDirectory, $"settings-{DateTime.Now:yyyyMMdd-HHmmss}.json"), true);
            settings = Read(_backupPath);
            if (settings is not null) File.Copy(_backupPath, _filePath, true);
        }
        settings ??= Read(_backupPath) ?? new AppSettings();
        settings.DisabledFeatures ??= [];
        settings.DisabledPlugins ??= [];
        settings.StartWithWindows = IsStartWithWindowsEnabled();
        if (settings.StartWithWindows) SetStartWithWindows(true);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        SetStartWithWindows(settings.StartWithWindows);
        var tempPath = _filePath + ".tmp";
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, settings, _jsonOptions);
                stream.Flush(true);
            }
            _ = Read(tempPath) ?? throw new InvalidDataException("设置文件写入校验失败。");
            if (File.Exists(_filePath)) File.Replace(tempPath, _filePath, _backupPath, true);
            else File.Move(tempPath, _filePath);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private AppSettings? Read(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<AppSettings>(stream, _jsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
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
