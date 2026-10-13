using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.HoursFilter;
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

    /// <summary>The hours filter of the toolbar. "Night average" heads the card with the night filter.</summary>
    public HoursFilterMode Hours { get; init; } = HoursFilterMode.AllDay;

    public ZoneShare? ZoneShare { get; init; }

    public IReadOnlyList<HourOfDayAverage>? TypicalDay { get; init; }
}
