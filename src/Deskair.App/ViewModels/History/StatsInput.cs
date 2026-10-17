using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.Pressure;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.History;

/// <summary>Everything the stats card is made from. Only the buckets are required; a part with no input (no earlier period, no zone share) is left out of the card.</summary>
/// <param name="Buckets">The buckets of the period shown.</param>
public sealed record StatsInput(IReadOnlyList<Bucket> Buckets)
{
    public Metric Metric { get; init; } = Metric.Co2;

    /// <summary>The range the toolbar shows (Day, Week, Month or Year). Names the period of the comparison.</summary>
    public Period Range { get; init; } = Period.Week;

    /// <summary>How the buckets are grouped (Day, Week or Month). Names the best and worst period.</summary>
    public Period Grouping { get; init; } = Period.Day;

    /// <summary>The buckets of the period before the one shown, for the change. Null or empty when there is nothing earlier.</summary>
    public IReadOnlyList<Bucket>? Previous { get; init; }

    public ZoneShare? ZoneShare { get; init; }

    /// <summary>How the readings of Temperature or Humidity spread below, inside and above the comfortable range. Null for other metrics.</summary>
    public ComfortShare? Comfort { get; init; }

    /// <summary>The biggest change in pressure over 24 hours in the period (Pressure only). Null for none.</summary>
    public PressureChange? PressureChange { get; init; }

    public IReadOnlyList<HourOfDayAverage>? TypicalDay { get; init; }

    /// <summary>The night hours from Settings. A worst window that falls mostly in them gets the "while you sleep" tip.</summary>
    public NightWindow? Night { get; init; }
}
