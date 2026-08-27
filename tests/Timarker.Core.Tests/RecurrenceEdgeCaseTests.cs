using Timarker.Models;

namespace Timarker.Core.Tests;

public sealed class RecurrenceEdgeCaseTests
{
    [Fact]
    public void AfterCount_StopsAfterConfiguredOccurrences()
    {
        var start = new DateTime(2026, 7, 1, 9, 0, 0);
        var item = Daily(start);
        item.RecurrenceEndMode = RecurrenceEndMode.AfterCount;
        item.RepeatCount = 2;

        item.Complete(start.AddMinutes(1));
        Assert.Equal(start.AddDays(1), item.NextDueAt(start.AddMinutes(1)));
        item.Complete(start.AddDays(1).AddMinutes(1));

        Assert.Null(item.NextDueAt(start.AddDays(1).AddMinutes(1)));
    }

    [Fact]
    public void OnDate_IncludesEndDateThenStops()
    {
        var start = new DateTime(2026, 7, 1, 9, 0, 0);
        var item = Daily(start);
        item.RecurrenceEndMode = RecurrenceEndMode.OnDate;
        item.RepeatUntil = start.AddDays(2);

        item.Complete(start.AddMinutes(1));
        item.Complete(start.AddDays(1).AddMinutes(1));
        Assert.Equal(start.AddDays(2), item.NextDueAt(start.AddDays(1).AddMinutes(1)));
        item.Complete(start.AddDays(2).AddMinutes(1));

        Assert.Null(item.NextDueAt(start.AddDays(2).AddMinutes(1)));
    }

    [Fact]
    public void Weekdays_SkipsWeekend()
    {
        var friday = new DateTime(2026, 7, 17, 9, 0, 0);
        var item = Daily(friday);
        item.RepeatPattern = RepeatPattern.Weekdays;

        item.Complete(friday.AddMinutes(1));

        Assert.Equal(new DateTime(2026, 7, 20, 9, 0, 0), item.NextDueAt(friday.AddMinutes(1)));
    }

    [Fact]
    public void MonthlyNthWeekday_FindsSecondMonday()
    {
        var item = new EventItem
        {
            Type = EventType.Recurring,
            StartAt = new DateTime(2026, 3, 1, 9, 0, 0),
            RepeatUnit = RepeatUnit.Month,
            RepeatPattern = RepeatPattern.MonthlyNthWeekday,
            RepeatWeekOfMonth = 2,
            RepeatDayOfWeek = DayOfWeek.Monday
        };

        Assert.Equal(new DateTime(2026, 3, 9, 9, 0, 0), item.NextDueAt(new DateTime(2026, 3, 1)));
    }

    [Fact]
    public void SkipToNext_DoesNotReturnMissedWeeklyOccurrence()
    {
        var item = new EventItem
        {
            Type = EventType.Recurring,
            StartAt = new DateTime(2026, 7, 13, 9, 0, 0),
            RepeatUnit = RepeatUnit.Week,
            MissedOccurrencePolicy = MissedOccurrencePolicy.SkipToNext
        };

        Assert.Equal(new DateTime(2026, 7, 20, 9, 0, 0), item.NextDueAt(new DateTime(2026, 7, 14, 12, 0, 0)));
    }

    [Fact]
    public void SkippingSpecificOccurrence_MovesToNextOne()
    {
        var start = new DateTime(2026, 7, 1, 9, 0, 0);
        var item = Daily(start);

        item.SkipOccurrenceAt(start);

        Assert.Equal(start.AddDays(1), item.NextDueAt(start));
        Assert.Equal(EventStatus.Skipped, item.Occurrences.Single().Status);
    }

    private static EventItem Daily(DateTime start) => new()
    {
        Type = EventType.Recurring,
        StartAt = start,
        RepeatUnit = RepeatUnit.Day
    };
}
