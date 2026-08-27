using Timarker.Models;
using Xunit;

namespace Timarker.Plugin.Tests;

public sealed class TodayTodoTests
{
    [Fact]
    public void SelectsOnlyCompletableItemsDueTodayAndPrioritizesHighPriority()
    {
        var now = new DateTime(2026, 8, 25, 10, 0, 0);
        var normal = new EventItem { Title = "普通", StartAt = now.Date.AddHours(18), Priority = EventPriority.Normal };
        var high = new EventItem { Title = "优先", StartAt = now.Date.AddHours(20), Priority = EventPriority.High };
        var tomorrow = new EventItem { Title = "明天", StartAt = now.Date.AddDays(1) };
        var done = new EventItem { Title = "完成", StartAt = now.Date, Status = EventStatus.Done };
        var anniversary = new EventItem { Title = "纪念日", Type = EventType.Anniversary, StartAt = now.Date };
        var handledRecurring = new EventItem
        {
            Title = "今天已完成的周期事项",
            Type = EventType.Recurring,
            StartAt = now.Date,
            RepeatUnit = RepeatUnit.Day,
            Occurrences = [new EventOccurrence { ScheduledAt = now.Date, Status = EventStatus.Done, HandledAt = now }]
        };

        var result = TodayTodoForm.SelectItems([normal, tomorrow, done, anniversary, handledRecurring, high], now);

        Assert.Equal([high, normal], result);
    }
}
