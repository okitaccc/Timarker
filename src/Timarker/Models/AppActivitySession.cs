namespace Timarker.Models;

public sealed class AppActivitySession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public string ProcessName { get; set; } = "";
    public string AppName { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public string Category { get; set; } = "未分类";
    public bool IsIdle { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? EventId { get; set; }

    public TimeSpan Duration => EndedAt > StartedAt ? EndedAt - StartedAt : TimeSpan.Zero;
}

public sealed class AppActivityRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProcessName { get; set; } = "";
    public string TitleContains { get; set; } = "";
    public string Category { get; set; } = "未分类";
    public Guid? ProjectId { get; set; }
    public Guid? EventId { get; set; }

    public bool Matches(string processName, string title) =>
        ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)
        && (string.IsNullOrWhiteSpace(TitleContains)
            || title.Contains(TitleContains, StringComparison.OrdinalIgnoreCase));
}

public static class ActivityInsights
{
    public static TimeSpan Overlap(AppActivitySession session, DateTime start, DateTime end)
    {
        var from = session.StartedAt > start ? session.StartedAt : start;
        var to = session.EndedAt < end ? session.EndedAt : end;
        return to > from ? to - from : TimeSpan.Zero;
    }

    public static IReadOnlyDictionary<string, TimeSpan> ByCategory(IEnumerable<AppActivitySession> sessions, DateTime start, DateTime end) =>
        sessions.Where(x => x.EndedAt > start && x.StartedAt < end)
            .GroupBy(x => x.IsIdle ? "空闲" : x.Category)
            .ToDictionary(x => x.Key, x => TimeSpan.FromTicks(x.Sum(s => Overlap(s, start, end).Ticks)));

    public static string DurationText(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours} 小时 {value.Minutes} 分钟"
        : $"{Math.Max(0, (int)Math.Round(value.TotalMinutes))} 分钟";
}
