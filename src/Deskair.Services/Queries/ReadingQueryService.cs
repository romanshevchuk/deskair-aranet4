using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
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

    private static readonly TimeSpan shortTypicalDayRange = TimeSpan.FromDays(7);
    private static readonly TimeSpan yearTypicalDayRange = TimeSpan.FromDays(90);

    /// <summary>Whether anything at all is stored for the device (the History screen shows a "collecting data" state until then).</summary>
    public async Task<bool> HasReadingsAsync(DeviceId device, CancellationToken cancellationToken)
    {
        return await readings.GetLastReadingTimeAsync(device, cancellationToken) is not null;
    }

    public async Task<IReadOnlyList<Reading>> GetRecentAsync(
        DeviceId device,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var recent = await readings.ReadRangeAsync(device, now - window, now, cancellationToken);

        return recent.OrderBy(reading => reading.Timestamp).ToList();
    }

    public async Task<IReadOnlyList<Bucket>> GetBucketsAsync(
        DeviceId device,
        Metric metric,
        DateTimeOffset start,
        DateTimeOffset end,
        Period grouping,
        HoursFilterMode filter,
        CancellationToken cancellationToken)
    {
        var inRange = await readings.ReadRangeAsync(device, start, end, cancellationToken);
        var filtered = HoursFilterRule.Apply(inRange, filter, calendar);
        var interval = devices.IntervalFor(device, end);

        return BucketAggregator.Group(filtered, metric, start, end, grouping, calendar, interval);
    }

    /// <summary>How the CO₂ readings in the range spread over the zones, after the hours filter. Null when there are none.</summary>
    public async Task<ZoneShare?> GetZoneShareAsync(
        DeviceId device,
        DateTimeOffset start,
        DateTimeOffset end,
        HoursFilterMode filter,
        CancellationToken cancellationToken)
    {
        var inRange = await readings.ReadRangeAsync(device, start, end, cancellationToken);

        return ZoneShareCalculator.Calculate(HoursFilterRule.Apply(inRange, filter, calendar));
    }

    public async Task<IReadOnlyList<HourOfDayAverage>> GetTypicalDayAsync(
        DeviceId device,
        Period period,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var (start, end) = period switch
        {
            Period.Month => (rangeStart, rangeEnd),
            Period.Year => (now - yearTypicalDayRange, now),
            _ => (now - shortTypicalDayRange, now)
        };

        var inRange = await readings.ReadRangeAsync(device, start, end, cancellationToken);

        return TypicalDayCalculator.HourlyAverages(inRange, calendar);
    }
}
