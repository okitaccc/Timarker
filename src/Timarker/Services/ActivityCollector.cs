using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Timarker.Models;

namespace Timarker.Services;

public sealed class ActivityCollector : IDisposable
{
    private readonly ActivityStore _store;
    private readonly AppSettings _settings;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 5000 };
    private AppActivitySession? _current;
    private DateTime _lastSampleAt = DateTime.Now;
    private DateTime _lastSavedAt = DateTime.MinValue;

    public ActivityCollector(ActivityStore store, AppSettings settings)
    {
        _store = store;
        _settings = settings;
        _timer.Tick += (_, _) => Sample();
        ApplySettings();
    }

    public bool IsPaused { get; private set; }
    public event EventHandler? ActivityChanged;

    public void ApplySettings()
    {
        if (_settings.ActivityTrackingEnabled && !IsPaused) _timer.Start();
        else
        {
            CloseCurrent(DateTime.Now);
            _timer.Stop();
        }
    }

    public void TogglePaused()
    {
        IsPaused = !IsPaused;
        ApplySettings();
    }

    public void ResetCurrent() => CloseCurrent(DateTime.Now);

    public void Sample()
    {
        if (!_settings.ActivityTrackingEnabled || IsPaused) return;
        var now = DateTime.Now;
        if (now - _lastSampleAt > TimeSpan.FromSeconds(30)) CloseCurrent(_lastSampleAt);
        _lastSampleAt = now;

        var idle = IdleTime() >= TimeSpan.FromMinutes(Math.Max(1, _settings.ActivityIdleMinutes));
        var snapshot = idle ? (Process: "idle", App: "空闲", Title: "") : ForegroundApp();
        if (!idle && IsShellSurface(snapshot.Process, snapshot.Title))
        {
            CloseCurrent(now);
            return;
        }
        if (Excluded(snapshot.Process))
        {
            CloseCurrent(now);
            return;
        }
        var title = _settings.ActivityStoreWindowTitles ? snapshot.Title : "";
        if (_current is null || _current.ProcessName != snapshot.Process || _current.IsIdle != idle || _current.WindowTitle != title)
        {
            CloseCurrent(now);
            var rule = _store.MatchRule(snapshot.Process, title);
            _current = new AppActivitySession
            {
                StartedAt = now,
                EndedAt = now,
                ProcessName = snapshot.Process,
                AppName = snapshot.App,
                WindowTitle = title,
                IsIdle = idle,
                Category = idle ? "空闲" : rule?.Category ?? "未分类",
                ProjectId = rule?.ProjectId,
                EventId = rule?.EventId
            };
            _store.Sessions.Add(_current);
            ActivityChanged?.Invoke(this, EventArgs.Empty);
        }
        else _current.EndedAt = now;

        if (now - _lastSavedAt >= TimeSpan.FromSeconds(30))
        {
            Save();
            ActivityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CloseCurrent(DateTime at)
    {
        if (_current is null) return;
        _current.EndedAt = at < _current.StartedAt ? _current.StartedAt : at;
        if (_current.Duration < TimeSpan.FromSeconds(30) && _store.Sessions.Count >= 2)
        {
            var previous = _store.Sessions[^2];
            if (previous.ProcessName == _current.ProcessName && previous.WindowTitle == _current.WindowTitle && previous.IsIdle == _current.IsIdle)
            {
                previous.EndedAt = _current.EndedAt;
                _store.Sessions.Remove(_current);
            }
        }
        _current = null;
        Save();
        ActivityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Save()
    {
        _store.Save(_settings.ActivityRetentionDays);
        _lastSavedAt = DateTime.Now;
    }

    private bool Excluded(string processName) =>
        processName.Equals("Timarker", StringComparison.OrdinalIgnoreCase)
        || _settings.ActivityExcludedProcesses.Any(x => x.Equals(processName, StringComparison.OrdinalIgnoreCase));

    internal static bool IsShellSurface(string processName, string title) =>
        processName.Equals("explorer", StringComparison.OrdinalIgnoreCase)
        && (string.IsNullOrWhiteSpace(title) || title.Equals("Program Manager", StringComparison.OrdinalIgnoreCase));

    private static (string Process, string App, string Title) ForegroundApp()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return ("system", "系统", "");
        GetWindowThreadProcessId(handle, out var processId);
        var title = new StringBuilder(512);
        _ = GetWindowText(handle, title, title.Capacity);
        try
        {
            using var process = Process.GetProcessById((int)processId);
            var name = process.ProcessName;
            string? display = null;
            try { display = process.MainModule?.FileVersionInfo.FileDescription; }
            catch { /* 提权进程可能不允许读取可执行文件信息，进程名仍可使用。 */ }
            return (name, string.IsNullOrWhiteSpace(display) ? name : display, title.ToString());
        }
        catch { return ("system", "系统应用", title.ToString()); }
    }

    private static TimeSpan IdleTime()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        return GetLastInputInfo(ref info)
            ? TimeSpan.FromMilliseconds(unchecked(Environment.TickCount - info.Time))
            : TimeSpan.Zero;
    }

    public void Dispose()
    {
        _timer.Stop();
        CloseCurrent(DateTime.Now);
        _timer.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo { public uint Size; public uint Time; }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInputInfo info);
}
