using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.HourGrid;
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

    /// <summary>The night hours from Settings → Sleep (22:00 to 07:00 without settings): the Night filter, the sleep card and the night band of the History grid.</summary>
    public NightWindow Night => settings?.Current.Night ?? NightWindow.Default;

    private static readonly TimeSpan typicalDayBeforeADay = TimeSpan.FromDays(7);

    /// <summary>Whether anything at all is stored for the device (the History screen shows a "collecting data" state until then).</summary>
    public async Task<bool> HasReadingsAsync(DeviceId device, CancellationToken cancellationToken)
    {
        return await readings.GetLastReadingTimeAsync(device, cancellationToken) is not null;
    }

    /// <summary>The time of the oldest stored reading, or null when nothing is stored: History goes back no further than the period before it (D-0019).</summary>
    public Task<DateTimeOffset?> GetFirstReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
        => readings.GetFirstReadingTimeAsync(device, cancellationToken);

    /// <summary>The readings from <paramref name="start"/> (inclusive) to <paramref name="end"/> (exclusive), oldest first: the line of any History day.</summary>
    public async Task<IReadOnlyList<Reading>> GetRangeAsync(DeviceId device, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var inRange = await readings.ReadRangeAsync(device, start, end, cancellationToken);

        return inRange.OrderBy(reading => reading.Timestamp).ToList();
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

    /// <summary>The History grid for Week and Month (D-0018): one row per local day of the range, 24 hourly means each.</summary>
    public async Task<IReadOnlyList<HourGridDay>> GetHourGridAsync(DeviceId device, Metric metric, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var inRange = await readings.ReadRangeAsync(device, start, end, cancellationToken);

        return HourGridCalculator.Build(inRange, metric, start, end, calendar);
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
    public Task<SleepSummary?> GetLastNightAsync(DeviceId device, Metric metric, CancellationToken cancellationToken)
        => GetLastNightAsync(device, metric, timeProvider.GetUtcNow(), cancellationToken);

    /// <summary>
    /// The night that last finished before <paramref name="at"/>: for a calendar day in History (at its end), the night that ended that morning
    /// (behaviour-rules.md "Sleep" rule 2).
    /// </summary>
    public async Task<SleepSummary?> GetLastNightAsync(DeviceId device, Metric metric, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var night = Night;

        if (night.IsEmpty)
        {
            return null;
        }

        var (start, end) = night.LastFinished(at, calendar);
        var inNight = await readings.ReadRangeAsync(device, start, end, cancellationToken);

        return SleepCalculator.Summarise(inNight, metric, start, end, devices.IntervalFor(device, end), calendar, ZoneThresholds.Default);
    }

    /// <summary>
    /// The mean per hour of day (behaviour-rules.md "Aggregation" rule 7): over the shown period, except Day, which uses the 7 days before the shown day,
    /// so a day is compared with the days before it and an earlier period with its own data.
    /// </summary>
    public async Task<IReadOnlyList<HourOfDayAverage>> GetTypicalDayAsync(
        DeviceId device,
        Period period,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        CancellationToken cancellationToken)
    {
        var (start, end) = period == Period.Day ? (rangeStart - typicalDayBeforeADay, rangeStart) : (rangeStart, rangeEnd);

        var inRange = await readings.ReadRangeAsync(device, start, end, cancellationToken);

        return TypicalDayCalculator.HourlyAverages(inRange, calendar);
    }
}
