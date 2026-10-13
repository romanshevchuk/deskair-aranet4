using Deskair.Domain.Readings;

namespace Deskair.Domain.Zones;

public static class ZoneShareCalculator
{
    /// <summary>How the readings spread over the zones, by number of readings. Readings with no CO₂ value are left out. Null when none are left.</summary>
    public static ZoneShare? Calculate(IEnumerable<Reading> readings, ZoneThresholds? thresholds = null)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var limits = thresholds ?? ZoneThresholds.Default;
        int good = 0, elevated = 0, high = 0;

        foreach (var reading in readings.Where(reading => !double.IsNaN(reading.Co2Ppm)))
        {
            switch (ZoneClassifier.Classify(reading.Co2Ppm, limits))
            {
                case Zone.Good:
                    good++;
                    break;
                case Zone.Elevated:
                    elevated++;
                    break;
                default:
                    high++;
                    break;
            }
        }

        var total = good + elevated + high;

        return total == 0 ? null : new ZoneShare((double)good / total, (double)elevated / total, (double)high / total);
    }
}
