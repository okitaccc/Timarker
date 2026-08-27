using Timarker.Models;

namespace Timarker.Core.Tests;

public sealed class AnytimeTodayTests
{
    [Fact]
    public void UsesEndOfDayAsCompletionBoundaryWithoutForcingReminder()
    {
        var item = new EventItem
        {
            Type = EventType.AnytimeToday,
            StartAt = new DateTime(2026, 8, 25, 9, 0, 0),
            ReminderEnabled = false
        };

        Assert.Equal(new DateTime(2026, 8, 25, 23, 59, 59, 999).AddTicks(9999), item.NextDueAt(new DateTime(2026, 8, 25, 12, 0, 0)));
        Assert.False(item.IsDue(new DateTime(2026, 8, 25, 12, 0, 0)));
    }

    [Fact]
    public void DailyAnytimeItemMovesToNextDayAfterCompletion()
    {
        var item = new EventItem
        {
            Type = EventType.AnytimeToday,
            StartAt = new DateTime(2026, 8, 25, 9, 0, 0),
            RepeatUnit = RepeatUnit.Day,
            ReminderEnabled = false
        };

        item.Complete(new DateTime(2026, 8, 25, 18, 0, 0));

        Assert.Equal(new DateTime(2026, 8, 26).AddDays(1).AddTicks(-1), item.NextDueAt(new DateTime(2026, 8, 25, 18, 0, 1)));
    }
}
