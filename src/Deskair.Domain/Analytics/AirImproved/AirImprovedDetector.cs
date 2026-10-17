using Deskair.Domain.Readings;

namespace Deskair.Domain.Analytics.AirImproved;

/// <summary>
/// Finds the "air improved" events in a run of readings (behaviour-rules.md "Air improved"). CO₂ at or above 1,000 ppm that falls by 150 ppm within 10 minutes starts an event.
/// The event ends when CO₂ is below 1,000, when it flattens (falls by less than 10 ppm per 5 minutes), or 45 minutes after it started. The History Day chart marks the starts,
/// at least 60 minutes apart.
/// </summary>
public static class AirImprovedDetector
{
    public const double LimitPpm = 1000;
    public const double DropPpm = 150;
    public static readonly TimeSpan DropWindow = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan MaximumLength = TimeSpan.FromMinutes(45);
    public static readonly TimeSpan MarkerSpacing = TimeSpan.FromMinutes(60);
    public const double FlatPerFiveMinutes = 10;

    /// <summary>Every event, in time order. The readings must be in time order. An event does not start inside another one.</summary>
    public static IReadOnlyList<AirImprovedEvent> Detect(IReadOnlyList<Reading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var events = new List<AirImprovedEvent>();
        var index = 0;

        while (index < readings.Count - 1)
        {
            if (Reached(readings, index) is not { } reached)
            {
                index++;
                continue;
            }

            var end = EndOf(readings, index, reached);

            events.Add(new AirImprovedEvent(
                readings[index].Timestamp,
                readings[index].Co2Ppm,
                readings[index].Co2Ppm - readings[end].Co2Ppm,
                readings[end].Timestamp - readings[index].Timestamp));
            index = end + 1;
        }

        return events;
    }

    /// <summary>The events to mark on the chart: a start less than 60 minutes after the last marked one is left out.</summary>
    public static IReadOnlyList<AirImprovedEvent> Markers(IReadOnlyList<AirImprovedEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var markers = new List<AirImprovedEvent>();

        foreach (var item in events)
        {
            if (markers.Count == 0 || item.Start - markers[^1].Start >= MarkerSpacing)
            {
                markers.Add(item);
            }
        }

        return markers;
    }

    /// <summary>The first later reading, within 10 minutes, that is 150 ppm or more below the one at <paramref name="start"/>, when that one is at or above 1,000.</summary>
    private static int? Reached(IReadOnlyList<Reading> readings, int start)
    {
        if (readings[start].Co2Ppm < LimitPpm)
        {
            return null;
        }

        for (var next = start + 1; next < readings.Count && readings[next].Timestamp - readings[start].Timestamp <= DropWindow; next++)
        {
            if (readings[start].Co2Ppm - readings[next].Co2Ppm >= DropPpm)
            {
                return next;
            }
        }

        return null;
    }

    /// <summary>The last reading of the event: from the one that reached the drop, up to the first reading that is below 1,000, flat, or 45 minutes after the start.</summary>
    private static int EndOf(IReadOnlyList<Reading> readings, int start, int reached)
    {
        var end = reached;

        if (readings[reached].Co2Ppm < LimitPpm)
        {
            return end;
        }

        for (var next = reached + 1; next < readings.Count; next++)
        {
            var before = readings[next - 1];
            var current = readings[next];

            if (current.Timestamp - readings[start].Timestamp > MaximumLength || FallPerFiveMinutes(before, current) < FlatPerFiveMinutes)
            {
                break;
            }

            end = next;

            if (current.Co2Ppm < LimitPpm)
            {
                break;
            }
        }

        return end;
    }

    private static double FallPerFiveMinutes(Reading before, Reading current)
    {
        var minutes = (current.Timestamp - before.Timestamp).TotalMinutes;

        return minutes <= 0 ? 0 : (before.Co2Ppm - current.Co2Ppm) * 5 / minutes;
    }
}
