using Deskair.Services.Alerts;

namespace Deskair.Tests.Services;

public class AlertEngineTests
{
    [Theory(Skip = "TODO(ALERT-01a)")]
    [InlineData(22, 0, true)]
    [InlineData(23, 30, true)]
    [InlineData(3, 0, true)]
    [InlineData(6, 59, true)]
    [InlineData(7, 0, false)]
    [InlineData(12, 0, false)]
    [InlineData(21, 59, false)]
    public void QuietHours_cross_midnight_and_the_end_is_exclusive(int hour, int minute, bool expected)
    {
        var time = new TimeOnly(hour, minute);

        Assert.Equal(expected, AlertEngine.IsQuietHour(time, new TimeOnly(22, 0), new TimeOnly(7, 0)));
    }

    [Theory(Skip = "TODO(ALERT-01a)")]
    [InlineData(9, 0, true)]
    [InlineData(12, 0, true)]
    [InlineData(17, 0, false)]
    [InlineData(8, 59, false)]
    public void QuietHours_within_one_day(int hour, int minute, bool expected)
    {
        var time = new TimeOnly(hour, minute);

        Assert.Equal(expected, AlertEngine.IsQuietHour(time, new TimeOnly(9, 0), new TimeOnly(17, 0)));
    }

    [Fact(Skip = "TODO(ALERT-01a)")]
    public void QuietHours_with_equal_start_and_end_are_off()
    {
        Assert.False(AlertEngine.IsQuietHour(new TimeOnly(12, 0), new TimeOnly(22, 0), new TimeOnly(22, 0)));
    }
}
