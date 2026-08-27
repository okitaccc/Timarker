namespace Timarker;

public sealed record FeatureModule(string Key, string Name, int Group, bool CanDisable = true);

public static class FeatureCatalog
{
    public static readonly IReadOnlyList<FeatureModule> All =
    [
        new("current", "今日", 0, false),
        new("editor", "新建事项", 0, false),
        new("calendar", "日历视图", 0),
        new("projects", "项目", 0),
        new("recommended", "下一步", 1),
        new("todo", "今日清单", 1),
        new("pomodoro", "番茄钟", 1),
        new("activity", "时间追踪", 2),
        new("history", "复盘", 2),
        new("statistics", "时间统计", 2),
        new("event-history", "事项历程", 2),
        new("folders", "收藏夹", 3),
        new("notes", "便签", 3),
        new("settings", "设置", 3, false)
    ];

    public static IEnumerable<FeatureModule> Optional => All.Where(x => x.CanDisable);
    public static string GroupName(int group) => group switch { 0 => "计划", 1 => "执行", 2 => "回顾", _ => "工具" };
}
