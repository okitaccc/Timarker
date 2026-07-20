using Timarker.Models;

namespace Timarker;

internal static class L
{
    public const string BrandZh = "事刻";
    public const string BrandEn = "Timarker";
    public const string Brand = "事刻 Timarker";

    public static bool IsEnglish { get; private set; }

    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["事刻 Timarker"] = "Timarker",
        ["事刻设置"] = "Timarker Settings",
        ["把重要的事，放进时间里"] = "Weave what matters into time",
        ["TIMARKER · 把重要的事，放进时间里"] = "TIMARKER · Mark what matters in time",
        ["设置"] = "Settings",
        ["语言"] = "Language",
        ["简体中文"] = "简体中文",
        ["关闭窗口时最小化到系统托盘"] = "Minimize to system tray when closing",
        ["开机后自动启动事刻"] = "Start Timarker with Windows",
        ["启用免打扰时段"] = "Enable quiet hours",
        ["免打扰开始"] = "Quiet hours start",
        ["免打扰结束"] = "Quiet hours end",
        ["默认提前提醒"] = "Default reminder lead",
        ["默认重复间隔"] = "Default repeat interval",
        ["默认重复次数"] = "Default repeat count",
        ["默认稍后提醒"] = "Default snooze",
        ["分钟"] = "min",
        ["次"] = "times",
        ["保存"] = "Save",
        ["取消"] = "Cancel",
        ["确定"] = "Confirm",
        ["显示主窗口"] = "Show Timarker",
        ["退出"] = "Exit",
        ["事项"] = "EVENTS",
        ["新建事务"] = "New event",
        ["当前事务"] = "Current events",
        ["推荐优先"] = "Recommended",
        ["日历视图"] = "Calendar",
        ["收藏夹"] = "Collections",
        ["番茄钟"] = "Pomodoro",
        ["新建事项"] = "New event",
        ["先填写必要信息，其他设置按需展开。"] = "Start with the essentials. Expand the rest when needed.",
        ["快速创建"] = "QUICK CREATE",
        ["事项名称"] = "Event name",
        ["快速设置"] = "Quick setup",
        ["事项用途"] = "Event type",
        ["日期与提醒"] = "DATE & REMINDERS",
        ["提醒方式"] = "Reminder policy",
        ["词条"] = "TAGS",
        ["常用词条"] = "Common tags",
        ["自定义词条"] = "Custom tags",
        ["已选择词条"] = "Selected tags",
        ["更多设置"] = "MORE OPTIONS",
        ["优先级"] = "Priority",
        ["加入收藏夹"] = "Add to collection",
        ["备注"] = "Notes",
        ["展开更多设置"] = "Show more options",
        ["收起更多设置"] = "Hide more options",
        ["添加到事刻"] = "Add to Timarker",
        ["今日与未来"] = "Today & upcoming",
        ["可按状态筛选，也可以打开悬浮倒计时。"] = "Filter by status or open the floating countdown.",
        ["筛选"] = "Filter",
        ["搜索"] = "Search",
        ["搜索标题/词条"] = "Search title or tag",
        ["例如：明天下午三点开会"] = "e.g. Team meeting tomorrow at 3 PM",
        ["备注，可不填"] = "Optional notes",
        ["例如：妈妈、我们、入职"] = "e.g. Mom, us, new job",
        ["例如：家人、伴侣、工作"] = "e.g. Family, partner, work",
        ["输入词条后按 Enter"] = "Type a tag and press Enter",
        ["普通事项"] = "Standard",
        ["周期事项"] = "Recurring",
        ["生日"] = "Birthday",
        ["纪念日"] = "Anniversary",
        ["设置开始时间"] = "Set start time",
        ["设置截止时间"] = "Set deadline",
        ["开始时间"] = "Start time",
        ["截止时间"] = "Deadline",
        ["选择日期与时间"] = "Choose date & time",
        ["选择生日日期与时间"] = "Choose birthday date & time",
        ["选择纪念日与时间"] = "Choose anniversary date & time",
        ["选择首次发生日期与时间"] = "Choose first occurrence",
        ["仅起始"] = "Start only",
        ["仅截止"] = "End only",
        ["日期区间"] = "Date range",
        ["开始日期"] = "Start date",
        ["截止日期"] = "End date",
        ["未选择起始日期"] = "No start date",
        ["未选择截止日期"] = "No deadline",
        ["重复"] = "Repeat",
        ["重复规则"] = "Repeat rule",
        ["再次提醒"] = "Remind again",
        ["最多提醒"] = "Maximum reminders",
        ["延后"] = "Snooze",
        ["每"] = "Every",
        ["尚未选择词条"] = "No tags selected",
        ["不使用模板"] = "No template",
        ["不设置"] = "None",
        ["全部"] = "All",
        ["今天"] = "Today",
        ["本周"] = "This week",
        ["逾期"] = "Overdue",
        ["待处理"] = "Pending",
        ["进行中"] = "In progress",
        ["已完成"] = "Completed",
        ["可能要做"] = "Maybe",
        ["到点开始"] = "Starts at",
        ["截止事项"] = "Due by",
        ["时间段"] = "Time range",
        ["习惯"] = "Habit",
        ["生日提醒"] = "Birthday reminder",
        ["已跳过"] = "Skipped",
        ["已延期"] = "Postponed",
        ["已逾期"] = "Overdue",
        ["已取消"] = "Cancelled",
        ["累计天数"] = "Day milestones",
        ["周年"] = "Yearly anniversary",
        ["天数与周年"] = "Days and years",
        ["阳历"] = "Solar",
        ["阴历"] = "Lunar",
        ["天"] = "day",
        ["周"] = "week",
        ["月"] = "month",
        ["年"] = "year",
        ["小时"] = "hours",
        ["编辑"] = "Edit",
        ["完成"] = "Complete",
        ["完成本次"] = "Complete this occurrence",
        ["跳过本次"] = "Skip this occurrence",
        ["结束重复"] = "End recurrence",
        ["添加到收藏夹"] = "Add to collection",
        ["新建收藏夹并加入"] = "Create collection and add",
        ["根据词条组合"] = "Create collection from tag",
        ["从收藏夹移除"] = "Remove from collection",
        ["暂无收藏夹"] = "No collections",
        ["该事件没有词条"] = "This event has no tags",
        ["删除"] = "Delete",
        ["关闭"] = "Close",
        ["倒计时"] = "Countdown",
        ["新建收藏夹"] = "New collection",
        ["从全部事件添加"] = "Add from all events",
        ["添加选中的事件"] = "Add selected events",
        ["重命名"] = "Rename",
        ["删除收藏夹"] = "Delete collection",
        ["选择一个收藏夹"] = "Select a collection",
        ["全部词条"] = "All tags",
        ["重命名收藏夹"] = "Rename collection",
        ["编辑事件"] = "Edit event",
        ["修改后会同步更新日历、收藏夹与提醒。"] = "Changes update the calendar, collections, and reminders.",
        ["基本信息"] = "BASIC INFO",
        ["词条与备注"] = "TAGS & NOTES",
        ["纪念日设置"] = "ANNIVERSARY",
        ["事项不能为空。"] = "Event name is required.",
        ["截止时间不能早于开始时间。"] = "The deadline cannot be earlier than the start time.",
        ["时间设置有误"] = "Invalid time range",
        ["选择方式"] = "Selection",
        ["时间"] = "Time",
        ["农历"] = "Lunar",
        ["农历  开"] = "Lunar  On",
        ["农历  关"] = "Lunar  Off",
        ["选择年月"] = "Choose month & year",
        ["这一天还没有事项"] = "No events on this day",
        ["暂无待处理事项"] = "No pending events",
        ["添加带时间的提醒后会显示在这里"] = "Timed reminders will appear here",
        ["提醒确认"] = "Reminder",
        ["现在需要处理这个事项吗？"] = "Do you want to handle this event now?",
        ["稍后/延期"] = "Snooze / postpone",
        ["稍后提醒"] = "Snooze",
        ["延期事项"] = "Postpone",
        ["忽略"] = "Ignore",
        ["未设置时间"] = "No time set",
        ["一次只做一件事。离开此页面后计时仍会继续。"] = "One thing at a time. The timer keeps running when you leave.",
        ["时间可随时调整，重置后生效。"] = "Adjust anytime; changes apply after reset.",
        ["专注中"] = "Focusing",
        ["休息中"] = "On a break",
        ["切到休息"] = "Start break",
        ["切到专注"] = "Start focus",
        ["专注"] = "Focus",
        ["休息"] = "Break",
        ["开始 / 暂停"] = "Start / pause",
        ["重置"] = "Reset",
        ["最小化到托盘"] = "Minimize to tray",
        ["退出程序"] = "Exit",
        ["显示年龄（已知出生年份）"] = "Show age (birth year known)",
        ["这是闰月生日"] = "Birthday is in a leap lunar month",
        ["人物或纪念对象"] = "Person or subject",
        ["关系或类别"] = "Relationship or category",
        ["生日历法"] = "Calendar",
        ["出生年份"] = "Birth year",
        ["农历闰月"] = "Lunar leap month",
        ["关闭事刻"] = "Close Timarker",
        ["关闭窗口后，你希望事刻怎么运行？"] = "What should Timarker do when you close the window?",
        ["最小化到系统托盘后，提醒会继续在后台工作；退出程序后，将不会再触发提醒。"] = "Reminders continue in the system tray. Exiting stops all reminders."
    };

    public static void Use(AppSettings settings) => IsEnglish = settings.Language.Equals("en", StringComparison.OrdinalIgnoreCase);

    public static string T(string text)
    {
        return IsEnglish && English.TryGetValue(text, out var translated) ? translated : text;
    }

    public static void Apply(Control root)
    {
        root.Text = T(root.Text);
        if (root is TextBox textBox) textBox.PlaceholderText = T(textBox.PlaceholderText);
        if (root.ContextMenuStrip is not null) Apply(root.ContextMenuStrip);
        foreach (Control child in root.Controls) Apply(child);
    }

    public static void Apply(ContextMenuStrip menu)
    {
        Apply(menu.Items);
        menu.Opening -= TranslateMenuOnOpening;
        menu.Opening += TranslateMenuOnOpening;
    }

    private static void TranslateMenuOnOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is ContextMenuStrip menu) Apply(menu.Items);
    }

    private static void Apply(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.Text = T(item.Text ?? string.Empty);
            if (item is ToolStripDropDownItem dropDown) Apply(dropDown.DropDownItems);
        }
    }
}
