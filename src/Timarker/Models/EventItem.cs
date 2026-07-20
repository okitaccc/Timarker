using System.Globalization;

namespace Timarker.Models;

public enum EventType
{
    StartAt,
    Deadline,
    TimeWindow,
    Recurring,
    Habit,
    Birthday,
    Anniversary,
    Maybe
}

public enum EventStatus
{
    Pending,
    InProgress,
    Done,
    Skipped,
    Postponed,
    Overdue,
    Cancelled
}

public enum EventPriority
{
    None,
    Low,
    Normal,
    High
}

public enum RepeatUnit
{
    None,
    Day,
    Week,
    Month,
    Year
}

public enum CalendarKind
{
    Solar,
    Lunar
}

public enum LeapDayRule
{
    February28,
    March1
}

public enum AnniversaryMode
{
    Days,
    Years,
    Both
}

public sealed class EventOccurrence
{
    public DateTime ScheduledAt { get; set; }
    public DateTime? AdjustedAt { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Pending;
    public DateTime? HandledAt { get; set; }

    public DateTime EffectiveAt => AdjustedAt ?? ScheduledAt;
    public bool IsHandled => Status is EventStatus.Done or EventStatus.Skipped or EventStatus.Cancelled;
}

public sealed class EventItem
{
    // Identity and content
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Tags { get; set; } = "";
    public string Categories { get; set; } = "";

    // Classification and state
    public EventType Type { get; set; } = EventType.StartAt;
    public EventStatus Status { get; set; } = EventStatus.Pending;
    public EventPriority Priority { get; set; } = EventPriority.Normal;
    public bool IsGroup { get; set; }
    public Guid? ParentId { get; set; }
    public List<Guid> FolderIds { get; set; } = [];

    public bool IsInFolder(Guid folderId) => ParentId == folderId || FolderIds.Contains(folderId);

    public void AddToFolder(Guid folderId)
    {
        if (!FolderIds.Contains(folderId))
        {
            FolderIds.Add(folderId);
        }
    }

    public void RemoveFromFolder(Guid folderId)
    {
        FolderIds.Remove(folderId);
        if (ParentId == folderId)
        {
            ParentId = null;
        }
    }
    // Schedule
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateTime? DeadlineAt { get; set; }
    public RepeatUnit RepeatUnit { get; set; } = RepeatUnit.None;
    public int RepeatEvery { get; set; } = 1;
    public List<EventOccurrence> Occurrences { get; set; } = [];
    public CalendarKind BirthdayCalendar { get; set; } = CalendarKind.Solar;
    public int? BirthdayMonth { get; set; }
    public int? BirthdayDay { get; set; }
    public bool BirthdayYearKnown { get; set; } = true;
    public bool BirthdayIsLeapMonth { get; set; }
    public LeapDayRule BirthdayLeapDayRule { get; set; } = LeapDayRule.February28;
    public string SubjectName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public AnniversaryMode AnniversaryMode { get; set; } = AnniversaryMode.Both;
    public string MilestoneDays { get; set; } = "10, 100, 365, 520, 1000";

    // Reminder policy and delivery state
    public int ReminderLeadMinutes { get; set; }
    public int ReminderRepeatMinutes { get; set; } = 10;
    public int ReminderRepeatCount { get; set; }
    public int ReminderSentCount { get; set; }
    public DateTime? LastReminderAt { get; set; }
    public DateTime? SnoozedUntil { get; set; }

    // Audit metadata
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsRecurringSeries => Type is EventType.Recurring or EventType.Habit
        || RepeatUnit is not RepeatUnit.None && Type is not (EventType.Birthday or EventType.Anniversary);

    public DateTime? NextDueAt(DateTime now)
    {
        if (Status is EventStatus.Cancelled || (!IsRecurringSeries && Status is (EventStatus.Done or EventStatus.Skipped)))
        {
            return null;
        }

        if (Type is EventType.Deadline)
        {
            return DeadlineAt;
        }

        if (Type is EventType.TimeWindow)
        {
            return StartAt;
        }

        if (Type is EventType.Birthday)
        {
            return NextBirthdayAt(now);
        }

        if (Type is EventType.Anniversary)
        {
            return NextAnniversaryAt(now);
        }

        if (Type is EventType.Recurring or EventType.Habit)
        {
            return NextRecurringAt(now);
        }

        return StartAt;
    }

    public bool IsDue(DateTime now)
    {
        if (SnoozedUntil is not null && SnoozedUntil > now)
        {
            return false;
        }

        var dueAt = ReminderTargetAt(now);
        if (dueAt is null)
        {
            return false;
        }

        var remindAt = dueAt.Value.AddMinutes(-ReminderLeadMinutes);
        if (now < remindAt)
        {
            return false;
        }

        if (LastReminderAt is null || LastReminderAt < remindAt)
        {
            return true;
        }

        return ReminderRepeatCount >= ReminderSentCount
            && ReminderRepeatMinutes > 0
            && LastReminderAt.Value.AddMinutes(ReminderRepeatMinutes) <= now;
    }

    public void MarkReminded(DateTime now)
    {
        var dueAt = ReminderTargetAt(now);
        var remindAt = dueAt?.AddMinutes(-ReminderLeadMinutes);
        ReminderSentCount = remindAt is not null && (LastReminderAt is null || LastReminderAt < remindAt)
            ? 1
            : ReminderSentCount + 1;
        LastReminderAt = now;
        UpdatedAt = now;
    }

    public void Complete(DateTime? handledAt = null)
    {
        var now = handledAt ?? DateTime.Now;
        if (IsRecurringSeries)
        {
            RecordOccurrence(EventStatus.Done, now);
            Status = EventStatus.Pending;
            ResetReminderState();
        }
        else
        {
            Status = EventStatus.Done;
            SnoozedUntil = null;
        }
        UpdatedAt = now;
    }

    public void SkipOccurrence(DateTime? handledAt = null)
    {
        var now = handledAt ?? DateTime.Now;
        if (IsRecurringSeries)
        {
            RecordOccurrence(EventStatus.Skipped, now);
            Status = EventStatus.Pending;
            ResetReminderState();
        }
        else
        {
            Status = EventStatus.Skipped;
            SnoozedUntil = null;
        }
        UpdatedAt = now;
    }

    public void EndRecurringSeries()
    {
        Status = EventStatus.Cancelled;
        SnoozedUntil = null;
        UpdatedAt = DateTime.Now;
    }

    public void SnoozeUntil(DateTime value)
    {
        SnoozedUntil = value;
        UpdatedAt = DateTime.Now;
    }

    public void ShiftSchedule(int minutes)
    {
        if (IsRecurringSeries)
        {
            var now = DateTime.Now;
            var scheduledAt = ActiveOccurrenceAt(now);
            if (scheduledAt is null) return;
            var occurrence = FindOccurrence(scheduledAt.Value) ?? new EventOccurrence { ScheduledAt = scheduledAt.Value };
            if (!Occurrences.Contains(occurrence)) Occurrences.Add(occurrence);
            occurrence.AdjustedAt = occurrence.EffectiveAt.AddMinutes(minutes);
            occurrence.Status = EventStatus.Pending;
            occurrence.HandledAt = null;
            ResetReminderState();
            UpdatedAt = now;
            return;
        }

        StartAt = StartAt?.AddMinutes(minutes);
        EndAt = EndAt?.AddMinutes(minutes);
        DeadlineAt = DeadlineAt?.AddMinutes(minutes);
        SnoozedUntil = null;
        ReminderSentCount = 0;
        LastReminderAt = null;
        Status = EventStatus.Pending;
        UpdatedAt = DateTime.Now;
    }

    private DateTime? ReminderTargetAt(DateTime now)
    {
        if (Type is EventType.Birthday)
        {
            return CurrentBirthdayAt(now);
        }

        if (Type is EventType.Anniversary)
        {
            return NextAnniversaryAt(now.AddMinutes(-ReminderLeadMinutes));
        }

        if (Type is EventType.Recurring or EventType.Habit)
        {
            return NextRecurringAt(now);
        }

        return NextDueAt(now);
    }

    public bool IsOverdue(DateTime now)
    {
        if (Status is EventStatus.Done or EventStatus.Skipped or EventStatus.Cancelled)
        {
            return false;
        }

        return DeadlineAt is not null && DeadlineAt < now;
    }

    public override string ToString()
    {
        var due = NextDueAt(DateTime.Now)?.ToString("yyyy-MM-dd HH:mm") ?? "未设置时间";
        return $"[{StatusText}] {due} - {Title}";
    }

    public string TypeText => Type switch
    {
        EventType.StartAt => "到点开始",
        EventType.Deadline => "截止事项",
        EventType.TimeWindow => "时间段",
        EventType.Recurring => "周期事项",
        EventType.Habit => "习惯",
        EventType.Birthday => "生日提醒",
        EventType.Anniversary => "纪念日",
        EventType.Maybe => "可能要做",
        _ => Type.ToString()
    };

    public string StatusText => Status switch
    {
        EventStatus.Pending => "待处理",
        EventStatus.InProgress => "进行中",
        EventStatus.Done => "已完成",
        EventStatus.Skipped => "已跳过",
        EventStatus.Postponed => "已延期",
        EventStatus.Overdue => "已逾期",
        EventStatus.Cancelled => "已取消",
        _ => Status.ToString()
    };

    public string PriorityText => Priority switch
    {
        EventPriority.None => "无",
        EventPriority.Low => "低",
        EventPriority.Normal => "普通",
        EventPriority.High => "高",
        _ => Priority.ToString()
    };

    public string CalendarText => BirthdayCalendar switch
    {
        CalendarKind.Solar => "阳历",
        CalendarKind.Lunar => "农历",
        _ => BirthdayCalendar.ToString()
    };

    public string AnniversarySummary(DateTime now)
    {
        if (StartAt is null)
        {
            return "未设置起始日期";
        }

        var days = Math.Max(0, (now.Date - StartAt.Value.Date).Days);
        var years = now.Year - StartAt.Value.Year;
        if (StartAt.Value.Date.AddYears(Math.Max(0, years)) > now.Date)
        {
            years--;
        }
        return AnniversaryMode switch
        {
            AnniversaryMode.Days => $"已经 {days} 天",
            AnniversaryMode.Years => $"第 {Math.Max(0, years)} 周年",
            _ => $"已经 {days} 天 · 第 {Math.Max(0, years)} 周年"
        };
    }

    public string BirthdaySummary(DateTime now)
    {
        var next = NextBirthdayAt(now);
        var person = string.IsNullOrWhiteSpace(SubjectName) ? "" : SubjectName.Trim();
        var relation = string.IsNullOrWhiteSpace(Relationship) ? "" : $" · {Relationship.Trim()}";
        var age = BirthdayYearKnown && StartAt is not null && next is not null
            ? $" · 将满 {Math.Max(0, next.Value.Year - StartAt.Value.Year)} 岁"
            : "";
        return $"{person}{relation}{age}".Trim(' ', '·');
    }

    private DateTime? NextAnniversaryAt(DateTime now)
    {
        if (StartAt is null)
        {
            return null;
        }

        var candidates = new List<DateTime>();
        if (AnniversaryMode is AnniversaryMode.Years or AnniversaryMode.Both)
        {
            var year = Math.Max(now.Year, StartAt.Value.Year + 1);
            var annual = StartAt.Value.AddYears(year - StartAt.Value.Year);
            if (annual < now)
            {
                annual = annual.AddYears(1);
            }
            candidates.Add(annual);
        }

        if (AnniversaryMode is AnniversaryMode.Days or AnniversaryMode.Both)
        {
            candidates.AddRange(ParseMilestones().Select(days => StartAt.Value.AddDays(days)).Where(date => date >= now));
        }
        return candidates.Count == 0 ? null : candidates.Min();
    }

    private IEnumerable<int> ParseMilestones() => MilestoneDays
        .Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(value => int.TryParse(value, out var days) ? days : 0)
        .Where(days => days > 0)
        .Distinct();

    private DateTime? NextRecurringAt(DateTime now)
    {
        if (StartAt is null || RepeatUnit is RepeatUnit.None)
        {
            return StartAt;
        }

        var adjusted = Occurrences
            .Where(x => !x.IsHandled && x.AdjustedAt is not null)
            .OrderBy(x => x.EffectiveAt)
            .FirstOrDefault();
        if (adjusted is not null) return adjusted.EffectiveAt;

        var current = CurrentRecurringAt(now);
        if (current is not null && FindOccurrence(current.Value)?.IsHandled is not true) return current;

        var next = current is null ? StartAt.Value : AddRepeat(current.Value);
        while (FindOccurrence(next)?.IsHandled is true)
        {
            var following = AddRepeat(next);
            if (following == next) break;
            next = following;
        }
        return next;
    }

    private DateTime? CurrentRecurringAt(DateTime now)
    {
        if (StartAt is null || RepeatUnit is RepeatUnit.None || StartAt > now)
        {
            return null;
        }

        var current = StartAt.Value;
        while (true)
        {
            var next = AddRepeat(current);

            if (next > now || next == current)
            {
                return current;
            }

            current = next;
        }
    }

    private DateTime AddRepeat(DateTime value) => RepeatUnit switch
    {
        RepeatUnit.Day => value.AddDays(RepeatEvery),
        RepeatUnit.Week => value.AddDays(7 * RepeatEvery),
        RepeatUnit.Month => value.AddMonths(RepeatEvery),
        RepeatUnit.Year => value.AddYears(RepeatEvery),
        _ => value
    };

    private DateTime? ActiveOccurrenceAt(DateTime now)
    {
        var adjusted = Occurrences
            .Where(x => !x.IsHandled && x.AdjustedAt is not null)
            .OrderBy(x => x.EffectiveAt)
            .FirstOrDefault();
        if (adjusted is not null) return adjusted.ScheduledAt;
        var current = CurrentRecurringAt(now);
        return current is not null && FindOccurrence(current.Value)?.IsHandled is not true
            ? current
            : NextRecurringAt(now);
    }

    private EventOccurrence? FindOccurrence(DateTime scheduledAt) => Occurrences
        .FirstOrDefault(x => x.ScheduledAt == scheduledAt);

    private void RecordOccurrence(EventStatus status, DateTime handledAt)
    {
        var scheduledAt = ActiveOccurrenceAt(handledAt);
        if (scheduledAt is null) return;
        var occurrence = FindOccurrence(scheduledAt.Value) ?? new EventOccurrence { ScheduledAt = scheduledAt.Value };
        if (!Occurrences.Contains(occurrence)) Occurrences.Add(occurrence);
        occurrence.Status = status;
        occurrence.HandledAt = handledAt;
    }

    private void ResetReminderState()
    {
        SnoozedUntil = null;
        ReminderSentCount = 0;
        LastReminderAt = null;
    }

    public void NormalizeAfterLoad()
    {
        FolderIds ??= [];
        Occurrences ??= [];
        RepeatEvery = Math.Max(1, RepeatEvery);
        if (IsRecurringSeries)
        {
            Type = Type is EventType.Habit ? EventType.Habit : EventType.Recurring;
            if (Status is EventStatus.Done or EventStatus.Skipped or EventStatus.Postponed) Status = EventStatus.Pending;
        }
    }

    private DateTime? NextBirthdayAt(DateTime now)
    {
        var current = CurrentBirthdayAt(now);
        if (current is not null && current >= now)
        {
            return current;
        }

        return BirthdayDateForYear(now.Year + 1)?.Add(DefaultTimeOfDay());
    }

    private DateTime? CurrentBirthdayAt(DateTime now)
    {
        var today = BirthdayDateForYear(now.Year)?.Add(DefaultTimeOfDay());
        if (today is null)
        {
            return null;
        }

        if (today.Value.Date < now.Date)
        {
            return BirthdayDateForYear(now.Year + 1)?.Add(DefaultTimeOfDay());
        }

        return today;
    }

    private DateTime? BirthdayDateForYear(int year)
    {
        if (BirthdayMonth is null || BirthdayDay is null)
        {
            return StartAt?.Date;
        }

        if (BirthdayCalendar is CalendarKind.Solar)
        {
            if (BirthdayMonth == 2 && BirthdayDay == 29 && !DateTime.IsLeapYear(year))
            {
                return BirthdayLeapDayRule is LeapDayRule.March1
                    ? new DateTime(year, 3, 1)
                    : new DateTime(year, 2, 28);
            }
            var day = Math.Min(BirthdayDay.Value, DateTime.DaysInMonth(year, BirthdayMonth.Value));
            return new DateTime(year, BirthdayMonth.Value, day);
        }

        try
        {
            var calendar = new ChineseLunisolarCalendar();
            var lunarYear = calendar.GetYear(new DateTime(year, 7, 1));
            var leapMonth = calendar.GetLeapMonth(lunarYear);
            var calendarMonth = BirthdayMonth.Value;
            if (BirthdayIsLeapMonth)
            {
                // 闰月只在部分年份存在；没有对应闰月时按同名普通月份提醒。
                if (leapMonth == BirthdayMonth.Value + 1) calendarMonth = leapMonth;
                else if (leapMonth > 0 && BirthdayMonth.Value >= leapMonth) calendarMonth++;
            }
            else if (leapMonth > 0 && BirthdayMonth.Value >= leapMonth)
            {
                calendarMonth++;
            }
            var day = Math.Min(BirthdayDay.Value, calendar.GetDaysInMonth(lunarYear, calendarMonth));
            return calendar.ToDateTime(lunarYear, calendarMonth, day, 0, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    private TimeSpan DefaultTimeOfDay()
    {
        return StartAt?.TimeOfDay ?? new TimeSpan(9, 0, 0);
    }
}
