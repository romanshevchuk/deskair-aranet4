using Deskair.Domain.Readings;

namespace Deskair.Services.Sync;

public static class SlotMerger
{
    public static DateTimeOffset Snap(DateTimeOffset timestamp, TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        var sinceEpoch = timestamp.UtcTicks - DateTime.UnixEpoch.Ticks;
        var shifted = sinceEpoch + interval.Ticks / 2;
        var slot = shifted / interval.Ticks - (shifted % interval.Ticks < 0 ? 1 : 0);

        return new DateTimeOffset(DateTime.UnixEpoch.Ticks + slot * interval.Ticks, TimeSpan.Zero);
    }

    public static IReadOnlyList<Reading> Merge(
        IReadOnlyList<Reading> existing,
        IReadOnlyList<Reading> incoming,
        TimeSpan interval)
    {
        var filled = existing
            .Where(IsUsable)
            .Select(reading => Snap(reading.Timestamp, interval))
            .ToHashSet();
        var added = new List<Reading>();

        foreach (var reading in incoming.Where(IsUsable))
        {
            var slot = Snap(reading.Timestamp, interval);

            if (filled.Add(slot))
            {
                added.Add(reading with { Timestamp = slot });
            }
        }

        return added.OrderBy(reading => reading.Timestamp).ToList();
    }

    private static bool IsUsable(Reading reading)
        => double.IsFinite(reading.Co2Ppm)
        && double.IsFinite(reading.TemperatureC)
        && double.IsFinite(reading.HumidityPct)
        && double.IsFinite(reading.PressureHpa);
}
