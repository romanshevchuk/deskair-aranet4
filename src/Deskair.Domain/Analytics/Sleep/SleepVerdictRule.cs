using Deskair.Domain.Zones;

namespace Deskair.Domain.Analytics.Sleep;

/// <summary>
/// The verdict on a night's air (docs/product/behaviour-rules.md "Sleep"): Good is an average below the Elevated limit and at most 1 hour at or above it;
/// Poor is an average above the High limit or 5 hours or more at or above the Elevated limit; everything else is Fair.
/// </summary>
public static class SleepVerdictRule
{
    private static readonly TimeSpan goodAtMost = TimeSpan.FromHours(1);
    private static readonly TimeSpan poorFrom = TimeSpan.FromHours(5);

    public static SleepVerdict Classify(double averagePpm, TimeSpan timeElevated, ZoneThresholds thresholds)
    {
        if (averagePpm > thresholds.HighAbovePpm || timeElevated >= poorFrom)
        {
            return SleepVerdict.Poor;
        }

        return averagePpm < thresholds.ElevatedFromPpm && timeElevated <= goodAtMost ? SleepVerdict.Good : SleepVerdict.Fair;
    }
}
