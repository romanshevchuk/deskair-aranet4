using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;

namespace Deskair.Domain.Analytics.Aggregation;

public static class BucketAggregator
{
    public static Bucket Aggregate(
        IReadOnlyList<Reading> readings,
        Metric metric,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan interval)
        => Aggregate(readings, metric, start, end, LocalCalendar.ExpectedSlots(start, end, interval));

    /// <summary>One bucket with the number of readings expected in it already worked out (fewer than the whole bucket holds when only some hours count).</summary>
    public static Bucket Aggregate(
        IReadOnlyList<Reading> readings,
        Metric metric,
        DateTimeOffset start,
        DateTimeOffset end,
        int expected)
    {
        var values = readings
            .Where(reading => reading.Timestamp >= start && reading.Timestamp < end)
            .Select(reading => reading.ValueOf(metric))
            .ToList();
        var state = BucketStateClassifier.Classify(values.Count, expected);

        if (values.Count == 0)
        {
            return new Bucket(start, end, null, null, null, 0, expected, state);
        }

        return new Bucket(start, end, values.Average(), values.Min(), values.Max(), values.Count, expected, state);
    }

    /// <summary>
    /// Groups readings into buckets. With <paramref name="onlyDuring"/> the readings are meant to be the ones inside those hours already (the Night filter), and a bucket expects readings
    /// only for the time of the night it holds, so a complete night is not "37 % present".
    /// </summary>
    public static IReadOnlyList<Bucket> Group(
        IReadOnlyList<Reading> readings,
        Metric metric,
        DateTimeOffset start,
        DateTimeOffset end,
        Period grouping,
        LocalCalendar calendar,
        TimeSpan interval,
        NightWindow? onlyDuring = null)
    {
        if (grouping == Period.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "A year is a range; group it by Week or Month.");
        }

        if (end <= start)
        {
            return [];
        }

        var edges = grouping == Period.Week
            ? WeekEdges(start, end, calendar)
            : DayOrMonthEdges(start, end, grouping, calendar);
        var sorted = readings.OrderBy(reading => reading.Timestamp).ToList();
        var buckets = new List<Bucket>(edges.Count - 1);
        var next = 0;

        for (var index = 0; index < edges.Count - 1; index++)
        {
            var from = edges[index];
            var to = edges[index + 1];
            var inBucket = new List<Reading>();

            while (next < sorted.Count && sorted[next].Timestamp < from)
            {
                next++;
            }

            while (next < sorted.Count && sorted[next].Timestamp < to)
            {
                inBucket.Add(sorted[next]);
                next++;
            }

            var expected = onlyDuring is { } night
                ? (int)(night.TimeWithin(from, to, calendar).Ticks / interval.Ticks)
                : LocalCalendar.ExpectedSlots(from, to, interval);

            buckets.Add(Aggregate(inBucket, metric, from, to, expected));
        }

        return buckets;
    }

    private static List<DateTimeOffset> DayOrMonthEdges(
        DateTimeOffset start,
        DateTimeOffset end,
        Period grouping,
        LocalCalendar calendar)
    {
        var edges = new List<DateTimeOffset> { start };
        var cursor = start;

        while (true)
        {
            var date = calendar.LocalDateOf(cursor);
            var nextDate = grouping == Period.Month
                ? new DateOnly(date.Year, date.Month, 1).AddMonths(1)
                : date.AddDays(1);
            var next = calendar.StartOfLocalDay(nextDate);

            if (next >= end)
            {
                edges.Add(end);
                return edges;
            }

            edges.Add(next);
            cursor = next;
        }
    }

    private static List<DateTimeOffset> WeekEdges(DateTimeOffset start, DateTimeOffset end, LocalCalendar calendar)
    {
        var lastDay = calendar.LocalDateOf(end - TimeSpan.FromTicks(1));
        var inside = new List<DateTimeOffset>();

        for (var weeksBack = 0; ; weeksBack++)
        {
            var edge = calendar.StartOfLocalDay(lastDay.AddDays(1 - 7 * weeksBack));

            if (edge <= start)
            {
                break;
            }

            if (edge < end)
            {
                inside.Add(edge);
            }
        }

        inside.Reverse();
        return [start, .. inside, end];
    }
}
