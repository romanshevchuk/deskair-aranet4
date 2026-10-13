using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.Sleep;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
using Deskair.Services.Devices;
using Deskair.Services.Settings;
using Deskair.Services.Storage;

namespace Deskair.Services.Queries;

/// <summary>The only way view models get data: range, filter and grouping in; buckets and stats out.</summary>
public sealed class ReadingQueryService
{
    private readonly IReadingStore readings;
    private readonly DeviceRegistry devices;
    private readonly TimeProvider timeProvider;
    private readonly LocalCalendar calendar;
    private readonly SettingsService? settings;

    public ReadingQueryService(
        IReadingStore readings,
        DeviceRegistry devices,
        TimeProvider timeProvider,
        LocalCalendar calendar,
        SettingsService? settings = null)
    {
        this.readings = readings;
        this.devices = devices;
        this.timeProvider = timeProvider;
        this.calendar = calendar;
        this.settings = settings;
    }

    /// <summary>The hours the Night filter and the sleep card use: the user's choice, or 22:00 to 07:00 when there are no settings.</summary>
    private NightWindow Night => settings?.Current.Night ?? NightWindow.Default;

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
        var filtered = HoursFilterRule.Apply(inRange, filter, calendar, Night);
        var interval = devices.IntervalFor(device, end);

        return BucketAggregator.Group(filtered, metric, start, end, grouping, calendar, interval, filter == HoursFilterMode.Night ? Night : null);
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

        return ZoneShareCalculator.Calculate(HoursFilterRule.Apply(inRange, filter, calendar, Night));
    }

    /// <summary>
    /// The summary of the most recent night that has ended (the night in progress is not "last night"), in the user's night hours. Null when the night window is empty.
    /// A night without readings still gives a summary, so the screen can say there is nothing.
    /// </summary>
    public async Task<SleepSummary?> GetLastNightAsync(DeviceId device, Metric metric, CancellationToken cancellationToken)
    {
        var night = Night;

        if (night.IsEmpty)
        {
            return null;
        }

        var (start, end) = night.LastFinished(timeProvider.GetUtcNow(), calendar);
        var inNight = await readings.ReadRangeAsync(device, start, end, cancellationToken);

        return SleepCalculator.Summarise(inNight, metric, start, end, devices.IntervalFor(device, end), calendar, ZoneThresholds.Default);
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
