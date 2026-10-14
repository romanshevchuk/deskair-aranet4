using Deskair.Domain.Readings;

namespace Deskair.Storage.Readings;

/// <summary>All readings of one device held in memory, sorted by time, so aggregation never touches the disk.</summary>
public sealed class ReadingIndex
{
    private readonly Lock gate = new();
    private List<Reading> items = [];

    public void Add(IReadOnlyList<Reading> readings)
    {
        if (readings.Count == 0)
        {
            return;
        }

        var incoming = readings.OrderBy(reading => reading.Timestamp).ToList();

        lock (gate)
        {
            var merged = new List<Reading>(items.Count + incoming.Count);
            var existing = 0;
            var added = 0;

            while (existing < items.Count || added < incoming.Count)
            {
                if (added >= incoming.Count || (existing < items.Count && items[existing].Timestamp <= incoming[added].Timestamp))
                {
                    merged.Add(items[existing++]);
                    continue;
                }

                var candidate = incoming[added++];

                if (merged.Count == 0 || merged[^1].Timestamp != candidate.Timestamp)
                {
                    merged.Add(candidate);
                }
            }

            items = merged;
        }
    }

    public IReadOnlyList<Reading> Range(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            return [];
        }

        lock (gate)
        {
            var first = FirstAtOrAfter(start);
            var last = FirstAtOrAfter(end);

            return items.GetRange(first, last - first);
        }
    }

    public DateTimeOffset? First()
    {
        lock (gate)
        {
            return items.Count == 0 ? null : items[0].Timestamp;
        }
    }

    public DateTimeOffset? Last()
    {
        lock (gate)
        {
            return items.Count == 0 ? null : items[^1].Timestamp;
        }
    }

    private int FirstAtOrAfter(DateTimeOffset moment)
    {
        var low = 0;
        var high = items.Count;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);

            if (items[middle].Timestamp < moment)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }
}
