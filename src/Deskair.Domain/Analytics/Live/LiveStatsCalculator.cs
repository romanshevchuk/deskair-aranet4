using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.Domain.Analytics.Live;

/// <summary>The three stats under the hero card (docs/product/behaviour-rules.md "Live values"). Readings are in time order, oldest first.</summary>
public static class LiveStatsCalculator
{
    /// <summary>The share of readings under the elevated limit, from 0 to 1. Readings stand for equal slots of time, so this is the share of time. Null without readings.</summary>
    public static double? GoodShare(IReadOnlyList<Reading> readings)
    {
        if (readings.Count == 0)
        {
            return null;
        }

        var good = readings.Count(reading => ZoneClassifier.Classify(reading.Co2Ppm) == Zone.Good);

        return (double)good / readings.Count;
    }

    /// <summary>The highest CO₂ value and when it was measured; the most recent one if several are equal. Null without readings.</summary>
    public static (double Ppm, DateTimeOffset At)? Peak(IReadOnlyList<Reading> readings)
    {
        if (readings.Count == 0)
        {
            return null;
        }

        var peak = readings[0];

        foreach (var reading in readings.Skip(1).Where(reading => reading.Co2Ppm >= peak.Co2Ppm))
        {
            peak = reading;
        }

        return (peak.Co2Ppm, peak.Timestamp);
    }

    /// <summary>The time since the zone last changed: from the first reading of the current run of readings in the same zone to <paramref name="now"/>. Null without readings.</summary>
    public static TimeSpan? TimeInCurrentZone(IReadOnlyList<Reading> readings, DateTimeOffset now)
    {
        if (readings.Count == 0)
        {
            return null;
        }

        var zone = ZoneClassifier.Classify(readings[^1].Co2Ppm);
        var runStart = readings.Count - 1;

        while (runStart > 0 && ZoneClassifier.Classify(readings[runStart - 1].Co2Ppm) == zone)
        {
            runStart--;
        }

        return now - readings[runStart].Timestamp;
    }
}
