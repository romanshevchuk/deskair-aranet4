namespace Deskair.Domain.Zones;

/// <summary>CO₂ zone limits in ppm. Fixed in v1 (docs/product/behaviour-rules.md).</summary>
public sealed record ZoneThresholds
{
    public ZoneThresholds(double elevatedFromPpm, double highAbovePpm)
    {
        if (double.IsNaN(elevatedFromPpm) || double.IsNaN(highAbovePpm))
        {
            throw new ArgumentException("Thresholds must be numbers.");
        }

        if (elevatedFromPpm >= highAbovePpm)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elevatedFromPpm),
                "The elevated limit must be below the high limit.");
        }

        ElevatedFromPpm = elevatedFromPpm;
        HighAbovePpm = highAbovePpm;
    }

    public static ZoneThresholds Default { get; } = new(1000, 1400);

    /// <summary>Good below this value; Elevated from this value up to <see cref="HighAbovePpm"/>.</summary>
    public double ElevatedFromPpm { get; }

    /// <summary>High above this value (the value itself is still Elevated).</summary>
    public double HighAbovePpm { get; }
}
