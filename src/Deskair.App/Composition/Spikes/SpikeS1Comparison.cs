using Deskair.Domain.Readings;

namespace Deskair.App.Composition.Spikes;

/// <summary>How well the sensor's readings line up with a phone export.</summary>
public sealed record SpikeS1ComparisonResult(
    int SensorCount,
    int ExportCount,
    int MatchedWithinInterval,
    int Co2Equal,
    TimeSpan MedianOffset,
    TimeSpan MaxOffset);

/// <summary>The best row-by-row alignment: export row = sensor row + Shift.</summary>
public sealed record SpikeS1Alignment(int Shift, int Compared, int Co2Equal, int AllEqual);

/// <summary>Spike S1 only: pairs each sensor reading with the nearest export reading and reports the time offsets.</summary>
public static class SpikeS1Comparison
{
    public static SpikeS1ComparisonResult Compare(IReadOnlyList<Reading> sensor, IReadOnlyList<Reading> export, TimeSpan interval)
    {
        var sortedExport = export.OrderBy(reading => reading.Timestamp).ToArray();
        var offsets = new List<TimeSpan>();
        var within = 0;
        var equal = 0;

        foreach (var reading in sensor)
        {
            var nearest = Nearest(sortedExport, reading.Timestamp);

            if (nearest is null)
            {
                continue;
            }

            var offset = (nearest.Value.Timestamp - reading.Timestamp).Duration();
            offsets.Add(offset);

            if (offset < interval)
            {
                within++;
            }

            if (Math.Abs(nearest.Value.Co2Ppm - reading.Co2Ppm) < 0.5)
            {
                equal++;
            }
        }

        offsets.Sort();

        return new SpikeS1ComparisonResult(
            sensor.Count,
            export.Count,
            within,
            equal,
            offsets.Count == 0 ? TimeSpan.Zero : offsets[offsets.Count / 2],
            offsets.Count == 0 ? TimeSpan.Zero : offsets[^1]);
    }

    private static Reading? Nearest(Reading[] sorted, DateTimeOffset time)
    {
        if (sorted.Length == 0)
        {
            return null;
        }

        var low = 0;
        var high = sorted.Length - 1;

        while (low < high)
        {
            var middle = (low + high) / 2;

            if (sorted[middle].Timestamp < time)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        var after = sorted[low];
        var before = sorted[Math.Max(low - 1, 0)];

        return (after.Timestamp - time).Duration() <= (before.Timestamp - time).Duration() ? after : before;
    }

    /// <summary>Tries every row shift up to <paramref name="maxShift"/> and returns the one where most readings agree on all four values (within the export's rounding).</summary>
    public static SpikeS1Alignment Align(IReadOnlyList<Reading> sensor, IReadOnlyList<Reading> export, int maxShift)
    {
        var sortedSensor = sensor.OrderBy(reading => reading.Timestamp).ToArray();
        var sortedExport = export.OrderBy(reading => reading.Timestamp).ToArray();
        var best = new SpikeS1Alignment(0, 0, 0, -1);

        for (var shift = -maxShift; shift <= maxShift; shift++)
        {
            var compared = 0;
            var co2 = 0;
            var all = 0;

            for (var index = 0; index < sortedSensor.Length; index++)
            {
                var other = index + shift;

                if (other < 0 || other >= sortedExport.Length)
                {
                    continue;
                }

                compared++;
                var left = sortedSensor[index];
                var right = sortedExport[other];

                if (Math.Abs(left.Co2Ppm - right.Co2Ppm) < 0.5)
                {
                    co2++;

                    if (Math.Abs(left.TemperatureC - right.TemperatureC) < 0.06
                        && Math.Abs(left.HumidityPct - right.HumidityPct) < 0.5
                        && Math.Abs(left.PressureHpa - right.PressureHpa) < 0.06)
                    {
                        all++;
                    }
                }
            }

            if (all > best.AllEqual)
            {
                best = new SpikeS1Alignment(shift, compared, co2, all);
            }
        }

        return best;
    }
}
