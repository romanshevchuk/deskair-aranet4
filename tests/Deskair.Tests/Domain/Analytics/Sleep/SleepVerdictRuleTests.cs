using Deskair.Domain.Analytics.Sleep;
using Deskair.Domain.Zones;

namespace Deskair.Tests.Domain.Analytics.Sleep;

public class SleepVerdictRuleTests
{
    private static readonly ZoneThresholds thresholds = ZoneThresholds.Default;

    [Theory]
    [InlineData(612, 0, SleepVerdict.Good)]
    [InlineData(999.9, 1, SleepVerdict.Good)]
    [InlineData(999.9, 1.01, SleepVerdict.Fair)]
    [InlineData(1000, 0, SleepVerdict.Fair)]
    [InlineData(1200, 3, SleepVerdict.Fair)]
    [InlineData(1400, 4.9, SleepVerdict.Fair)]
    [InlineData(1400.1, 0, SleepVerdict.Poor)]
    [InlineData(900, 5, SleepVerdict.Poor)]
    [InlineData(1200, 7, SleepVerdict.Poor)]
    public void The_verdict_follows_the_average_and_the_hours_above_the_limit(double average, double hours, SleepVerdict expected)
    {
        Assert.Equal(expected, SleepVerdictRule.Classify(average, TimeSpan.FromHours(hours), thresholds));
    }

    [Fact]
    public void Poor_wins_over_good_when_both_could_apply()
    {
        // An average under 1,000 with six hours above it: possible with a very low rest of the night.
        Assert.Equal(SleepVerdict.Poor, SleepVerdictRule.Classify(950, TimeSpan.FromHours(6), thresholds));
    }
}
