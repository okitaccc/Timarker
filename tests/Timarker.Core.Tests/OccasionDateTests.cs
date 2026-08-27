using Timarker.Models;

namespace Timarker.Core.Tests;

public sealed class OccasionDateTests
{
    [Theory]
    [InlineData(LeapDayRule.February28, 2027, 2, 28)]
    [InlineData(LeapDayRule.March1, 2027, 3, 1)]
    public void LeapDayBirthday_UsesConfiguredNonLeapYearRule(LeapDayRule rule, int year, int month, int day)
    {
        var item = Birthday(2, 29, new DateTime(2000, 2, 29, 9, 0, 0));
        item.BirthdayLeapDayRule = rule;

        Assert.Equal(new DateTime(year, month, day, 9, 0, 0), item.NextDueAt(new DateTime(2026, 3, 2)));
    }

    [Fact]
    public void SolarBirthday_AfterThisYearsDateMovesToNextYear()
    {
        var item = Birthday(7, 21, new DateTime(2000, 7, 21, 9, 0, 0));

        Assert.Equal(new DateTime(2027, 7, 21, 9, 0, 0), item.NextDueAt(new DateTime(2026, 7, 22)));
    }

    [Fact]
    public void LunarNewYear_ConvertsToExpectedSolarDate()
    {
        var item = Birthday(1, 1, new DateTime(2000, 2, 5, 9, 0, 0));
        item.BirthdayCalendar = CalendarKind.Lunar;

        Assert.Equal(new DateTime(2027, 2, 6, 9, 0, 0), item.NextDueAt(new DateTime(2026, 3, 1)));
    }

    [Fact]
    public void AnniversaryBothMode_ChoosesEarlierMilestone()
    {
        var item = new EventItem
        {
            Type = EventType.Anniversary,
            StartAt = new DateTime(2026, 1, 1, 9, 0, 0),
            AnniversaryMode = AnniversaryMode.Both,
            MilestoneDays = "10, 100"
        };

        Assert.Equal(new DateTime(2026, 1, 11, 9, 0, 0), item.NextDueAt(new DateTime(2026, 1, 5)));
    }

    [Fact]
    public void CompletedAnniversaryMilestone_MovesToFollowingMilestone()
    {
        var item = new EventItem
        {
            Type = EventType.Anniversary,
            StartAt = new DateTime(2026, 1, 1, 9, 0, 0),
            AnniversaryMode = AnniversaryMode.Days,
            MilestoneDays = "10, 100"
        };
        item.Complete(new DateTime(2026, 1, 11, 10, 0, 0));

        Assert.Equal(new DateTime(2026, 4, 11, 9, 0, 0), item.NextDueAt(new DateTime(2026, 1, 11, 10, 0, 0)));
    }

    private static EventItem Birthday(int month, int day, DateTime start) => new()
    {
        Type = EventType.Birthday,
        StartAt = start,
        BirthdayCalendar = CalendarKind.Solar,
        BirthdayMonth = month,
        BirthdayDay = day
    };
}
