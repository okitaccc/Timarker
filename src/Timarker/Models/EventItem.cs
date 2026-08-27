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
    Maybe,
    AnytimeToday
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

public enum RepeatPattern
{
    Interval,
    Weekdays,
    SelectedWeekdays,
    MonthlyNthWeekday,
    MonthlyLastDay,
    LunarYearly
}

public enum RecurrenceEndMode
{
    Never,
    OnDate,
    AfterCount
}

public enum MissedOccurrencePolicy
{
    RemindLatest,
    SkipToNext
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
    public bool IsProject { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? ProjectId { get; set; }
    public int ProjectOrder { get; set; }
    public List<Guid> FolderIds { get; set; } = [];
    public List<Guid> PersonIds { get; set; } = [];

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
    public RepeatPattern RepeatPattern { get; set; }
    public List<DayOfWeek> RepeatDaysOfWeek { get; set; } = [];
    public int RepeatWeekOfMonth { get; set; } = 1;
    public DayOfWeek RepeatDayOfWeek { get; set; } = DayOfWeek.Monday;
    public RecurrenceEndMode RecurrenceEndMode { get; set; }
    public DateTime? RepeatUntil { get; set; }
    public int RepeatCount { get; set; }
    public bool IsRecurrencePaused { get; set; }
    public MissedOccurrencePolicy MissedOccurrencePolicy { get; set; }
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
    public bool ReminderEnabled { get; set; } = true;
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
        if (Status is EventStatus.Cancelled
            || IsRecurringSeries && IsRecurrencePaused
            || (!IsRecurringSeries && Status is (EventStatus.Done or EventStatus.Skipped)))
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

        if (Type is EventType.AnytimeToday)
        {
            var occurrence = IsRecurringSeries ? NextRecurringAt(now) : StartAt;
            return occurrence?.Date.AddDays(1).AddTicks(-1);
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
        if (!ReminderEnabled) return false;
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
        if (IsRecurringSeries || Type is EventType.Birthday or EventType.Anniversary)
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
        if (IsRecurringSeries || Type is EventType.Birthday or EventType.Anniversary)
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

    public void SetRecurrencePaused(bool paused)
    {
        IsRecurrencePaused = paused;
        ResetReminderState();
        UpdatedAt = DateTime.Now;
    }

    public void SkipOccurrenceAt(DateTime scheduledAt)
    {
        var occurrence = FindOccurrence(scheduledAt) ?? new EventOccurrence { ScheduledAt = scheduledAt };
        if (!Occurrences.Contains(occurrence)) Occurrences.Add(occurrence);
        occurrence.Status = EventStatus.Skipped;
        occurrence.HandledAt = DateTime.Now;
        ResetReminderState();
        UpdatedAt = DateTime.Now;
    }

    public void EndBefore(DateTime occurrence)
    {
        RecurrenceEndMode = RecurrenceEndMode.OnDate;
        RepeatUntil = occurrence.Date.AddDays(-1);
        UpdatedAt = DateTime.Now;
    }

    public void ResetAsNewSeries()
    {
        Id = Guid.NewGuid();
        Occurrences = [];
        Status = EventStatus.Pending;
        IsRecurrencePaused = false;
        ReminderSentCount = 0;
        LastReminderAt = null;
        SnoozedUntil = null;
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    public void DetachFromSeries()
    {
        ResetAsNewSeries();
        Type = DeadlineAt is not null && StartAt is not null
            ? EventType.TimeWindow
            : DeadlineAt is not null
                ? EventType.Deadline
                : EventType.StartAt;
        RepeatUnit = RepeatUnit.None;
        RepeatPattern = RepeatPattern.Interval;
        RepeatDaysOfWeek = [];
        RecurrenceEndMode = RecurrenceEndMode.Never;
        RepeatUntil = null;
        RepeatCount = 0;
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
        if (!ReminderEnabled) return null;

        if (Type is EventType.AnytimeToday)
        {
            return IsRecurringSeries ? NextRecurringAt(now) : StartAt;
        }
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
        EventType.StartAt => "一次性事项",
        EventType.Deadline => "截止事项",
        EventType.TimeWindow => "时间段",
        EventType.Recurring => "周期事项",
        EventType.Habit => "习惯",
        EventType.Birthday => "生日提醒",
        EventType.Anniversary => "纪念日",
        EventType.Maybe => "可能要做",
        EventType.AnytimeToday => "当天内完成",
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
            if (annual.Date < now.Date || IsOccurrenceHandled(annual))
            {
                annual = annual.AddYears(1);
            }
            candidates.Add(annual);
        }

        if (AnniversaryMode is AnniversaryMode.Days or AnniversaryMode.Both)
        {
            candidates.AddRange(ParseMilestones()
                .Select(days => StartAt.Value.AddDays(days))
                .Where(date => date.Date >= now.Date && !IsOccurrenceHandled(date)));
        }
        return candidates.Count == 0 ? null : candidates.Min();
    }

    private IEnumerable<int> ParseMilestones() => MilestoneDays
        .Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(value => int.TryParse(value, out var days) ? days : 0)
        .Where(days => days > 0)
        .Distinct();

    public DateTime? CurrentOrNextOccurrenceAt(DateTime now) => NextRecurringAt(now);

    public bool OccursOn(DateTime day) => OccurrenceOn(day) is not null;

    public DateTime? OccurrenceOn(DateTime day)
    {
        if (!IsRecurringSeries || Status is EventStatus.Cancelled)
        {
            var due = NextDueAt(day.Date.AddDays(1).AddTicks(-1));
            return due?.Date == day.Date ? due : null;
        }

        var occurrence = FirstRecurringAt();
        var index = 1;
        var end = day.Date.AddDays(1);
        while (occurrence is not null && occurrence < end && index <= 100_000)
        {
            if (!IsOccurrenceAllowed(occurrence.Value, index)) return null;
            var stored = FindOccurrence(occurrence.Value);
            if ((stored?.EffectiveAt ?? occurrence.Value).Date == day.Date) return occurrence;
            occurrence = NextRecurringAfter(occurrence.Value);
            index++;
        }
        return Occurrences.FirstOrDefault(x => x.EffectiveAt.Date == day.Date)?.ScheduledAt;
    }

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
        if (current is not null
            && MissedOccurrencePolicy is MissedOccurrencePolicy.SkipToNext
            && current.Value.ScheduledAt.Date < now.Date)
        {
            var future = NextRecurringAfter(current.Value.ScheduledAt);
            var futureIndex = current.Value.Index + 1;
            while (future is not null && future.Value.Date < now.Date && IsOccurrenceAllowed(future.Value, futureIndex))
            {
                future = NextRecurringAfter(future.Value);
                futureIndex++;
            }
            return future is not null && IsOccurrenceAllowed(future.Value, futureIndex) ? future : null;
        }
        if (current is not null && FindOccurrence(current.Value.ScheduledAt)?.IsHandled is not true)
        {
            return current.Value.ScheduledAt;
        }

        var next = current is null ? FirstRecurringAt() : NextRecurringAfter(current.Value.ScheduledAt);
        var index = current?.Index + 1 ?? 1;
        while (next is not null && IsOccurrenceAllowed(next.Value, index) && FindOccurrence(next.Value)?.IsHandled is true)
        {
            next = NextRecurringAfter(next.Value);
            index++;
        }
        return next is not null && IsOccurrenceAllowed(next.Value, index) ? next : null;
    }

    private (DateTime ScheduledAt, int Index)? CurrentRecurringAt(DateTime now)
    {
        if (StartAt is null || RepeatUnit is RepeatUnit.None || StartAt > now)
        {
            return null;
        }

        var current = FirstRecurringAt();
        var index = 1;
        (DateTime ScheduledAt, int Index)? latest = null;
        while (current is not null && current <= now && index <= 100_000)
        {
            if (!IsOccurrenceAllowed(current.Value, index)) break;
            latest = (current.Value, index);
            var next = NextRecurringAfter(current.Value);
            if (next is null || next == current) break;
            current = next;
            index++;
        }
        return latest;
    }

    private DateTime? FirstRecurringAt()
    {
        if (StartAt is null) return null;
        var start = StartAt.Value;
        return RepeatPattern switch
        {
            RepeatPattern.Weekdays => NextMatchingDay(start, day => day is not (DayOfWeek.Saturday or DayOfWeek.Sunday), true),
            RepeatPattern.SelectedWeekdays => NextMatchingDay(start, day => EffectiveRepeatDays().Contains(day), true),
            RepeatPattern.MonthlyNthWeekday => FirstMonthlyOccurrence(start),
            RepeatPattern.MonthlyLastDay => FirstMonthlyLastDay(start),
            _ => start
        };
    }

    private DateTime? NextRecurringAfter(DateTime value) => RepeatPattern switch
    {
        RepeatPattern.Weekdays => NextMatchingDay(value.AddDays(1), day => day is not (DayOfWeek.Saturday or DayOfWeek.Sunday), true),
        RepeatPattern.SelectedWeekdays => NextMatchingDay(value.AddDays(1), day => EffectiveRepeatDays().Contains(day), true),
        RepeatPattern.MonthlyNthWeekday => NextMonthlyOccurrence(value, RepeatWeekOfMonth, RepeatDayOfWeek),
        RepeatPattern.MonthlyLastDay => NextMonthlyLastDay(value),
        RepeatPattern.LunarYearly => NextLunarYearly(value),
        _ => AddRepeat(value)
    };

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
        return current is not null && FindOccurrence(current.Value.ScheduledAt)?.IsHandled is not true
            ? current.Value.ScheduledAt
            : NextRecurringAt(now);
    }

    private bool IsOccurrenceAllowed(DateTime occurrence, int index) => RecurrenceEndMode switch
    {
        RecurrenceEndMode.OnDate => RepeatUntil is null || occurrence.Date <= RepeatUntil.Value.Date,
        RecurrenceEndMode.AfterCount => index <= Math.Max(1, RepeatCount),
        _ => true
    };

    private HashSet<DayOfWeek> EffectiveRepeatDays() => RepeatDaysOfWeek.Count == 0
        ? [StartAt?.DayOfWeek ?? DayOfWeek.Monday]
        : RepeatDaysOfWeek.ToHashSet();

    private static DateTime NextMatchingDay(DateTime value, Func<DayOfWeek, bool> matches, bool includeCurrent)
    {
        var candidate = includeCurrent ? value : value.AddDays(1);
        for (var i = 0; i < 7; i++, candidate = candidate.AddDays(1))
        {
            if (matches(candidate.DayOfWeek)) return candidate;
        }
        return value;
    }

    private DateTime NextMonthlyOccurrence(DateTime value, int week, DayOfWeek day)
    {
        var month = new DateTime(value.Year, value.Month, 1).AddMonths(Math.Max(1, RepeatEvery));
        return MonthlyOccurrence(month.Year, month.Month, week, day, value.TimeOfDay);
    }

    private DateTime FirstMonthlyOccurrence(DateTime start)
    {
        var result = MonthlyOccurrence(start.Year, start.Month, RepeatWeekOfMonth, RepeatDayOfWeek, start.TimeOfDay);
        return result < start ? NextMonthlyOccurrence(start, RepeatWeekOfMonth, RepeatDayOfWeek) : result;
    }

    private DateTime NextMonthlyLastDay(DateTime value)
    {
        var month = new DateTime(value.Year, value.Month, 1).AddMonths(Math.Max(1, RepeatEvery));
        return new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month)).Add(value.TimeOfDay);
    }

    private DateTime FirstMonthlyLastDay(DateTime start)
    {
        var result = new DateTime(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month)).Add(start.TimeOfDay);
        return result < start ? NextMonthlyLastDay(start) : result;
    }

    private static DateTime MonthlyOccurrence(int year, int month, int week, DayOfWeek day, TimeSpan time)
    {
        if (week < 0)
        {
            var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            return last.AddDays(-((7 + (int)last.DayOfWeek - (int)day) % 7)).Add(time);
        }
        var first = new DateTime(year, month, 1);
        var result = first.AddDays((7 + (int)day - (int)first.DayOfWeek) % 7 + 7 * (Math.Clamp(week, 1, 5) - 1));
        if (result.Month != month) result = result.AddDays(-7);
        return result.Add(time);
    }

    private DateTime? NextLunarYearly(DateTime value)
    {
        if (StartAt is null) return null;
        try
        {
            var calendar = new ChineseLunisolarCalendar();
            var sourceYear = calendar.GetYear(StartAt.Value);
            var sourceMonth = calendar.GetMonth(StartAt.Value);
            var sourceDay = calendar.GetDayOfMonth(StartAt.Value);
            var leapMonth = calendar.GetLeapMonth(sourceYear);
            var logicalMonth = leapMonth > 0 && sourceMonth >= leapMonth ? sourceMonth - 1 : sourceMonth;
            var wasLeap = leapMonth > 0 && sourceMonth == leapMonth;
            for (var year = value.Year + 1; year <= value.Year + 3; year++)
            {
                var lunarYear = calendar.GetYear(new DateTime(year, 7, 1));
                var targetLeap = calendar.GetLeapMonth(lunarYear);
                var targetMonth = logicalMonth;
                if (wasLeap && targetLeap == logicalMonth + 1) targetMonth = targetLeap;
                else if (targetLeap > 0 && logicalMonth >= targetLeap) targetMonth++;
                var day = Math.Min(sourceDay, calendar.GetDaysInMonth(lunarYear, targetMonth));
                var result = calendar.ToDateTime(lunarYear, targetMonth, day, 0, 0, 0, 0).Add(StartAt.Value.TimeOfDay);
                if (result > value) return result;
            }
        }
        catch
        {
            // 超出农历支持范围时停止该系列。
        }
        return null;
    }

    private EventOccurrence? FindOccurrence(DateTime scheduledAt) => Occurrences
        .FirstOrDefault(x => x.ScheduledAt == scheduledAt);

    private void RecordOccurrence(EventStatus status, DateTime handledAt)
    {
        var scheduledAt = Type is EventType.Birthday or EventType.Anniversary
            ? NextDueAt(handledAt.Date)
            : ActiveOccurrenceAt(handledAt);
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
        PersonIds ??= [];
        Occurrences ??= [];
        RepeatDaysOfWeek ??= [];
        RepeatEvery = Math.Max(1, RepeatEvery);
        RepeatWeekOfMonth = RepeatWeekOfMonth is < -1 or 0 or > 5 ? 1 : RepeatWeekOfMonth;
        RepeatCount = Math.Max(0, RepeatCount);
        ProjectOrder = Math.Max(0, ProjectOrder);
        if (ProjectId == Id) ProjectId = null;
        if (IsProject)
        {
            IsGroup = false;
            ProjectId = null;
            RepeatUnit = RepeatUnit.None;
        }
        if (IsRecurringSeries)
        {
            Type = Type is EventType.Habit or EventType.AnytimeToday ? Type : EventType.Recurring;
            if (Status is EventStatus.Done or EventStatus.Skipped or EventStatus.Postponed) Status = EventStatus.Pending;
        }
    }

    private DateTime? NextBirthdayAt(DateTime now)
    {
        var current = CurrentBirthdayAt(now);
        if (current is not null && current.Value.Date >= now.Date && !IsOccurrenceHandled(current.Value))
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

    private bool IsOccurrenceHandled(DateTime scheduledAt) => Occurrences.Any(occurrence =>
        occurrence.IsHandled && occurrence.ScheduledAt.Date == scheduledAt.Date);
}
