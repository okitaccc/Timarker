using Timarker.Models;

namespace Timarker.Core.Tests;

public sealed class ActivityInsightsTests
{
    [Fact]
    public void Overlap_ClipsSessionToRequestedDay()
    {
        var day = new DateTime(2026, 8, 10);
        var session = new AppActivitySession { StartedAt = day.AddMinutes(-30), EndedAt = day.AddMinutes(45) };

        Assert.Equal(TimeSpan.FromMinutes(45), ActivityInsights.Overlap(session, day, day.AddDays(1)));
    }

    [Fact]
    public void ByCategory_SeparatesIdleTime()
    {
        var start = new DateTime(2026, 8, 10);
        var sessions = new[]
        {
            new AppActivitySession { StartedAt = start, EndedAt = start.AddMinutes(30), Category = "工作" },
            new AppActivitySession { StartedAt = start.AddMinutes(30), EndedAt = start.AddMinutes(40), Category = "工作", IsIdle = true }
        };

        var result = ActivityInsights.ByCategory(sessions, start, start.AddDays(1));

        Assert.Equal(TimeSpan.FromMinutes(30), result["工作"]);
        Assert.Equal(TimeSpan.FromMinutes(10), result["空闲"]);
    }

    [Fact]
    public void Rule_WithTitle_IsMoreSpecificThanProcessOnlyRule()
    {
        var broad = new AppActivityRule { ProcessName = "chrome", Category = "浏览" };
        var specific = new AppActivityRule { ProcessName = "chrome", TitleContains = "GitHub", Category = "工作" };

        Assert.True(broad.Matches("Chrome", "GitHub - Timarker"));
        Assert.True(specific.Matches("chrome", "GitHub - Timarker"));
        Assert.False(specific.Matches("chrome", "视频网站"));
    }
}
