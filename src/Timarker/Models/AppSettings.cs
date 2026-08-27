namespace Timarker.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "zh-CN";
    public bool CloseToTray { get; set; } = true;
    public bool CloseToTrayPromptDismissed { get; set; }
    public bool StartWithWindows { get; set; }
    public bool ShowTodayTodoOnStartup { get; set; } = true;
    public bool QuietHoursEnabled { get; set; }
    public TimeSpan QuietHoursStart { get; set; } = new(22, 0, 0);
    public TimeSpan QuietHoursEnd { get; set; } = new(8, 0, 0);
    public int DefaultReminderLeadMinutes { get; set; }
    public int DefaultReminderRepeatMinutes { get; set; } = 10;
    public int DefaultReminderRepeatCount { get; set; } = 1;
    public int DefaultSnoozeMinutes { get; set; } = 10;
    public string Theme { get; set; } = "system";
    public int FontScalePercent { get; set; } = 100;
    public string BackgroundImagePath { get; set; } = "";
    public string BackgroundImageLayout { get; set; } = "fill";
    public int BackgroundOpacity { get; set; } = 35;
    public List<string> DisabledFeatures { get; set; } = [];
    public List<string> DisabledPlugins { get; set; } = [];
    public int? MainWindowX { get; set; }
    public int? MainWindowY { get; set; }
    public int? MainWindowWidth { get; set; }
    public int? MainWindowHeight { get; set; }
    public bool MainWindowMaximized { get; set; }
    public bool ActivityTrackingEnabled { get; set; }
    public bool ActivityStoreWindowTitles { get; set; }
    public int ActivityIdleMinutes { get; set; } = 5;
    public int ActivityRetentionDays { get; set; } = 365;
    public List<string> ActivityExcludedProcesses { get; set; } = [];

    public bool FeatureEnabled(string key) => !DisabledFeatures.Contains(key, StringComparer.OrdinalIgnoreCase);
}
