using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Analytics.HourGrid;
using Deskair.Domain.Analytics.Sleep;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.History;

/// <summary>Everything one refresh of the History screen read from the queries, before any of it is shown. A part the chosen metric or range does not need is null.</summary>
internal sealed record HistoryLoad(
    DateTimeOffset? First,
    int MaxOffset,
    HistoryPeriod Window,
    DateTimeOffset Now,
    Metric Metric,
    Period Range,
    Period Grouping,
    TimeSpan Interval,
    IReadOnlyList<Bucket> Buckets,
    IReadOnlyList<Bucket> Previous,
    IReadOnlyList<HourOfDayAverage> TypicalDay,
    ZoneShare? ZoneShare,
    ComfortShare? Comfort,
    IReadOnlyList<Reading>? Recent,
    SleepSummary? LastNight,
    NightsSummary? Nights,
    IReadOnlyList<HourGridDay>? GridDays,
    IReadOnlyList<HourGridMonth>? GridMonths,
    PressureSeries? Pressure);
