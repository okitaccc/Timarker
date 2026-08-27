using Timarker.Models;

namespace Timarker.Core.Tests;

public sealed class RecurringEventTests
{
    [Fact]
    public void DailySeries_MovesToNextOccurrenceAfterCompletion()
    {
        var item = Recurring(new DateTime(2026, 7, 1, 9, 0, 0), RepeatUnit.Day, every: 2);

        item.Complete(new DateTime(2026, 7, 1, 10, 0, 0));

        Assert.Equal(new DateTime(2026, 7, 3, 9, 0, 0), item.NextDueAt(new DateTime(2026, 7, 1, 10, 0, 0)));
        Assert.Equal(EventStatus.Pending, item.Status);
    }

    [Fact]
    public void SelectedWeekdays_SkipsUnselectedDays()
    {
        var item = Recurring(new DateTime(2026, 7, 14, 9, 0, 0), RepeatUnit.Week);
        item.RepeatPattern = RepeatPattern.SelectedWeekdays;
        item.RepeatDaysOfWeek = [DayOfWeek.Wednesday, DayOfWeek.Friday];

        Assert.Equal(new DateTime(2026, 7, 15, 9, 0, 0), item.NextDueAt(new DateTime(2026, 7, 14, 8, 0, 0)));
        Assert.True(item.OccursOn(new DateTime(2026, 7, 17)));
        Assert.False(item.OccursOn(new DateTime(2026, 7, 16)));
    }

    [Fact]
    public void MonthlyLastDay_HandlesDifferentMonthLengths()
    {
        var item = Recurring(new DateTime(2026, 1, 31, 9, 0, 0), RepeatUnit.Month);
        item.RepeatPattern = RepeatPattern.MonthlyLastDay;
        item.Complete(new DateTime(2026, 1, 31, 10, 0, 0));

        Assert.Equal(new DateTime(2026, 2, 28, 9, 0, 0), item.NextDueAt(new DateTime(2026, 1, 31, 10, 0, 0)));
    }

    [Fact]
    public void PausedSeries_HasNoNextOccurrence()
    {
        var item = Recurring(new DateTime(2026, 7, 1, 9, 0, 0), RepeatUnit.Day);

        item.SetRecurrencePaused(true);

        Assert.Null(item.NextDueAt(new DateTime(2026, 7, 2)));
    }

    private static EventItem Recurring(DateTime start, RepeatUnit unit, int every = 1) => new()
    {
        Type = EventType.Recurring,
        StartAt = start,
        RepeatUnit = unit,
        RepeatEvery = every,
        Status = EventStatus.Pending
    };
}
