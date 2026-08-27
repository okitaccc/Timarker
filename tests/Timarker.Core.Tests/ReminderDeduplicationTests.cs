using Timarker.Models;

namespace Timarker.Core.Tests;

public sealed class ReminderDeduplicationTests
{
    [Fact]
    public void OneTimeEvent_CanDisableReminder()
    {
        var item = OneTime(new DateTime(2026, 8, 26, 9, 0, 0));
        item.ReminderEnabled = false;

        Assert.Equal("一次性事项", item.TypeText);
        Assert.False(item.IsDue(new DateTime(2026, 8, 26, 9, 0, 0)));
    }

    [Fact]
    public void MarkingReminder_PreventsImmediateDuplicate()
    {
        var dueAt = new DateTime(2026, 7, 28, 9, 0, 0);
        var item = OneTime(dueAt);

        Assert.True(item.IsDue(dueAt));
        item.MarkReminded(dueAt);

        Assert.False(item.IsDue(dueAt));
        Assert.False(item.IsDue(dueAt.AddMinutes(30)));
        Assert.Equal(1, item.ReminderSentCount);
    }

    [Fact]
    public void RepeatReminder_FiresOnlyAtConfiguredIntervalsAndCount()
    {
        var dueAt = new DateTime(2026, 7, 28, 9, 0, 0);
        var item = OneTime(dueAt);
        item.ReminderRepeatMinutes = 10;
        item.ReminderRepeatCount = 2;

        item.MarkReminded(dueAt);
        Assert.False(item.IsDue(dueAt.AddMinutes(9)));
        Assert.True(item.IsDue(dueAt.AddMinutes(10)));
        item.MarkReminded(dueAt.AddMinutes(10));
        Assert.True(item.IsDue(dueAt.AddMinutes(20)));
        item.MarkReminded(dueAt.AddMinutes(20));

        Assert.False(item.IsDue(dueAt.AddMinutes(30)));
        Assert.Equal(3, item.ReminderSentCount);
    }

    [Fact]
    public void Snooze_BlocksReminderUntilSnoozeTime()
    {
        var dueAt = new DateTime(2026, 7, 28, 9, 0, 0);
        var item = OneTime(dueAt);
        item.SnoozeUntil(dueAt.AddMinutes(15));

        Assert.False(item.IsDue(dueAt.AddMinutes(14)));
        Assert.True(item.IsDue(dueAt.AddMinutes(15)));
    }

    [Fact]
    public void NewRecurringOccurrence_StartsANewReminderCycle()
    {
        var first = new DateTime(2026, 7, 28, 9, 0, 0);
        var item = new EventItem
        {
            Type = EventType.Recurring,
            StartAt = first,
            RepeatUnit = RepeatUnit.Day,
            ReminderRepeatCount = 0
        };
        item.MarkReminded(first);
        item.Complete(first.AddMinutes(1));

        var next = first.AddDays(1);
        Assert.True(item.IsDue(next));
        item.MarkReminded(next);
        Assert.Equal(1, item.ReminderSentCount);
    }

    private static EventItem OneTime(DateTime dueAt) => new()
    {
        Type = EventType.StartAt,
        StartAt = dueAt,
        ReminderRepeatCount = 0,
        ReminderRepeatMinutes = 10
    };
}
