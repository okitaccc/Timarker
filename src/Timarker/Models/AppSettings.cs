namespace Timarker.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "zh-CN";
    public bool CloseToTray { get; set; } = true;
    public bool CloseToTrayPromptDismissed { get; set; }
    public bool StartWithWindows { get; set; }
    public bool QuietHoursEnabled { get; set; }
    public TimeSpan QuietHoursStart { get; set; } = new(22, 0, 0);
    public TimeSpan QuietHoursEnd { get; set; } = new(8, 0, 0);
    public int DefaultReminderLeadMinutes { get; set; }
    public int DefaultReminderRepeatMinutes { get; set; } = 10;
    public int DefaultReminderRepeatCount { get; set; } = 1;
    public int DefaultSnoozeMinutes { get; set; } = 10;
}
