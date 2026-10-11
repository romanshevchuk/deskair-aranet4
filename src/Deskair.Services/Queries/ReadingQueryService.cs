using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Storage;

namespace Deskair.Services.Queries;

/// <summary>The only way view models get data: range, filter and grouping in; buckets and stats out.</summary>
public sealed class ReadingQueryService
{
    private readonly IReadingStore readings;
    private readonly DeviceRegistry devices;
    private readonly TimeProvider timeProvider;
    private readonly LocalCalendar calendar;

    public ReadingQueryService(
        IReadingStore readings,
        DeviceRegistry devices,
        TimeProvider timeProvider,
        LocalCalendar calendar)
    {
        this.readings = readings;
        this.devices = devices;
        this.timeProvider = timeProvider;
        this.calendar = calendar;
    }

    // TODO(QUERY-01a) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Sync and data" rule 4 (Live shows the last 24 h).
    // Input : a device, a window length ending now.
    // Output: readings in [now - window, now], oldest first.
    // Rules : "now" comes from TimeProvider; read through IReadingStore.ReadRangeAsync.
    // Edge  : no readings returns an empty list.
    // Done  : add Services/ReadingQueryServiceTests.Live_* with a fake store and clock.
    public Task<IReadOnlyList<Reading>> GetRecentAsync(
        DeviceId device,
        TimeSpan window,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(QUERY-01a)");

    // TODO(QUERY-01b) [M, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rules 1 to 5, 7.
    // Input : device, metric, range start and end, grouping, hours filter.
    // Output: buckets from BucketAggregator.Group over readings filtered by HoursFilterRule.
    // Rules : apply the hours filter before grouping; the interval comes from DeviceRegistry.
    // Edge  : no data returns buckets that are all NoData.
    // Done  : add Services/ReadingQueryServiceTests.Buckets_* with fakes.
    public Task<IReadOnlyList<Bucket>> GetBucketsAsync(
        DeviceId device,
        Metric metric,
        DateTimeOffset start,
        DateTimeOffset end,
        Period grouping,
        HoursFilterMode filter,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(QUERY-01b)");

    // TODO(QUERY-01c) [M, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 6.
    // Input : device, period (Day, Week, Month, Year), the selected range for Month.
    // Output: 24 hourly averages from TypicalDayCalculator.
    // Rules : Day and Week use the last 7 days, Month the selected range, Year the last 90 days.
    // Edge  : no data returns 24 empty hours.
    // Done  : add Services/ReadingQueryServiceTests.TypicalDay_* with fakes.
    public Task<IReadOnlyList<HourOfDayAverage>> GetTypicalDayAsync(
        DeviceId device,
        Period period,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(QUERY-01c)");
}
