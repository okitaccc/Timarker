using Timarker.Models;

namespace Timarker.Core.Tests;

public sealed class EventLifecycleTests
{
    [Fact]
    public void CompletingOneTimeEvent_EndsIt()
    {
        var item = new EventItem { StartAt = new DateTime(2026, 7, 1, 9, 0, 0) };

        item.Complete(new DateTime(2026, 7, 1, 10, 0, 0));

        Assert.Equal(EventStatus.Done, item.Status);
        Assert.Null(item.NextDueAt(new DateTime(2026, 7, 2)));
    }

    [Fact]
    public void CompletingRecurringEvent_RecordsOccurrenceAndKeepsSeriesPending()
    {
        var start = new DateTime(2026, 7, 1, 9, 0, 0);
        var item = new EventItem { Type = EventType.Recurring, StartAt = start, RepeatUnit = RepeatUnit.Day };

        item.Complete(start.AddMinutes(1));

        Assert.Equal(EventStatus.Pending, item.Status);
        Assert.Equal(EventStatus.Done, item.Occurrences.Single().Status);
        Assert.Equal(start.AddDays(1), item.NextDueAt(start.AddMinutes(1)));
    }

    [Fact]
    public void EndingSeries_CancelsFutureOccurrences()
    {
        var item = new EventItem { Type = EventType.Recurring, StartAt = DateTime.Today, RepeatUnit = RepeatUnit.Day };

        item.EndRecurringSeries();

        Assert.Equal(EventStatus.Cancelled, item.Status);
        Assert.Null(item.NextDueAt(DateTime.Today));
    }

    [Fact]
    public void DetachingOccurrence_CreatesIndependentEvent()
    {
        var originalId = Guid.NewGuid();
        var item = new EventItem { Id = originalId, Type = EventType.Recurring, StartAt = DateTime.Today, RepeatUnit = RepeatUnit.Day };

        item.DetachFromSeries();

        Assert.NotEqual(originalId, item.Id);
        Assert.Equal(EventType.StartAt, item.Type);
        Assert.Equal(RepeatUnit.None, item.RepeatUnit);
    }

    [Fact]
    public void ShiftingOneTimeEvent_MovesAllTimesAndResetsReminderState()
    {
        var start = new DateTime(2026, 7, 1, 9, 0, 0);
        var item = new EventItem
        {
            StartAt = start,
            EndAt = start.AddHours(1),
            DeadlineAt = start.AddHours(2),
            LastReminderAt = start,
            ReminderSentCount = 2,
            Status = EventStatus.Postponed
        };

        item.ShiftSchedule(30);

        Assert.Equal(start.AddMinutes(30), item.StartAt);
        Assert.Equal(start.AddHours(1).AddMinutes(30), item.EndAt);
        Assert.Equal(start.AddHours(2).AddMinutes(30), item.DeadlineAt);
        Assert.Null(item.LastReminderAt);
        Assert.Equal(0, item.ReminderSentCount);
        Assert.Equal(EventStatus.Pending, item.Status);
    }
}
