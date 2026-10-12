using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.Domain.Analytics.Ribbon;

/// <summary>The 24 h ribbon: 144 slots of ten minutes ending at a given time, each in the zone of the average CO₂ of its readings. A slot without readings is left out.</summary>
public static class RibbonCalculator
{
    public const int SlotCount = 144;

    private static readonly TimeSpan slotLength = TimeSpan.FromMinutes(10);

    public static IReadOnlyList<ZoneSpan> Build(IReadOnlyList<Reading> readings, DateTimeOffset end)
    {
        var start = end - (slotLength * SlotCount);
        var sums = new double[SlotCount];
        var counts = new int[SlotCount];

        foreach (var reading in readings.Where(reading => reading.Timestamp >= start && reading.Timestamp <= end))
        {
            // A reading exactly on the end belongs to the last slot.
            var slot = Math.Min(SlotCount - 1, (int)((reading.Timestamp - start).Ticks / slotLength.Ticks));

            sums[slot] += reading.Co2Ppm;
            counts[slot]++;
        }

        var spans = new List<ZoneSpan>();

        for (var slot = 0; slot < SlotCount; slot++)
        {
            if (counts[slot] > 0)
            {
                var from = start + (slotLength * slot);

                spans.Add(new ZoneSpan(from, from + slotLength, ZoneClassifier.Classify(sums[slot] / counts[slot])));
            }
        }

        return spans;
    }
}
