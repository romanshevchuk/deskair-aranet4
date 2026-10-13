using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.App.ViewModels.Data;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Queries;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.History;

public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly HistoryToolbarViewModel toolbar;
    private readonly StatsViewModel stats;
    private readonly TypicalDayViewModel typicalDay;
    private readonly ReadingQueryService query;
    private readonly DeviceRegistry devices;
    private readonly LocalCalendar calendar;
    private readonly TimeProvider timeProvider;
    private readonly IMessenger messenger;
    private readonly ILogger<HistoryViewModel> logger;
    private readonly MetricFormatter formatter;
    private CancellationTokenSource? current;
    private bool following;

    [ObservableProperty]
    private IReadOnlyList<ChartBar>? bars;

    [ObservableProperty]
    private IReadOnlyList<ChartPoint>? points;

    [ObservableProperty]
    private IReadOnlyList<ChartGap>? gaps;

    /// <summary>Saturdays and Sundays inside the range, shaded behind bars that stand for single days. Null otherwise.</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartGap>? weekendSpans;

    /// <summary>Nothing is stored for the selected room yet, or there is no room: the screen shows "collecting your first data" instead of charts.</summary>
    [ObservableProperty]
    private bool isEmpty;

    public HistoryViewModel(
        HistoryToolbarViewModel toolbar,
        StatsViewModel stats,
        TypicalDayViewModel typicalDay,
        ExportViewModel export,
        ReadingQueryService query,
        DeviceRegistry devices,
        LocalCalendar calendar,
        TimeProvider timeProvider,
        IMessenger messenger,
        MetricFormatter formatter,
        ILogger<HistoryViewModel> logger)
    {
        this.toolbar = toolbar;
        this.stats = stats;
        this.typicalDay = typicalDay;
        Export = export;
        this.query = query;
        this.devices = devices;
        this.calendar = calendar;
        this.timeProvider = timeProvider;
        this.messenger = messenger;
        this.logger = logger;
        this.formatter = formatter;
    }

    public HistoryToolbarViewModel Toolbar => toolbar;

    public StatsViewModel Stats => stats;

    public TypicalDayViewModel TypicalDay => typicalDay;

    /// <summary>Saves all readings of the selected room to a file. The toolbar shows its button.</summary>
    public ExportViewModel Export { get; }

    /// <summary>
    /// Loads the screen now, in the background; a failure is logged. The first call also starts following the toolbar and the app: a changed choice,
    /// a finished sync, another room or another unit loads the screen again.
    /// </summary>
    public void Refresh()
    {
        Follow();
        _ = RefreshQuietlyAsync();
    }


    /// <summary>Asks the app to open the import (Settings → Data and storage). The empty screen offers it.</summary>
    [RelayCommand]
    private void Import()
    {
        messenger.Send(new ImportRequested());
    }

    private void Follow()
    {
        if (following)
        {
            return;
        }

        following = true;
        toolbar.PropertyChanged += (_, _) => Refresh();
        messenger.Register<HistoryViewModel, SyncCompleted>(this, static (self, _) => self.Refresh());
        messenger.Register<HistoryViewModel, ReadingsImported>(this, static (self, _) => self.Refresh());
        messenger.Register<HistoryViewModel, DeviceSelected>(this, static (self, _) => self.Refresh());
        messenger.Register<HistoryViewModel, SettingsChanged>(this, static (self, _) => self.Refresh());
    }

    private async Task RefreshQuietlyAsync()
    {
        try
        {
            await RefreshAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Loading the History screen failed.");
            }
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        current?.Cancel();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        current = source;

        try
        {
            await LoadAsync(source.Token);
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // A newer refresh replaced this one, so its results are no longer wanted.
        }
        finally
        {
            if (ReferenceEquals(current, source))
            {
                current = null;
            }
        }
    }

    private async Task LoadAsync(CancellationToken token)
    {
        if (devices.Selected is not { } device)
        {
            Clear();
            return;
        }

        var range = toolbar.Range;
        var end = timeProvider.GetUtcNow();
        var start = StartOf(range, end);
        var metric = toolbar.Metric;
        var grouping = range == Period.Week ? Period.Day : toolbar.Grouping;
        var interval = devices.IntervalFor(device, end);

        var hasReadings = await query.HasReadingsAsync(device, token);

        var buckets = await Task.Run(
            () => query.GetBucketsAsync(device, metric, start, end, grouping, toolbar.Hours, token),
            token);
        var hours = await Task.Run(() => query.GetTypicalDayAsync(device, range, start, end, token), token);
        var previous = await Task.Run(
            () => query.GetBucketsAsync(device, metric, StartOfPeriodBefore(range, start), start, grouping, toolbar.Hours, token),
            token);
        var zoneShare = metric == Metric.Co2 ? await query.GetZoneShareAsync(device, start, end, toolbar.Hours, token) : null;
        var recent = range == Period.Day ? await query.GetRecentAsync(device, end - start, token) : null;

        token.ThrowIfCancellationRequested();

        IsEmpty = !hasReadings;
        stats.Update(new StatsInput(buckets)
        {
            Metric = metric,
            Range = range,
            Grouping = grouping,
            Previous = previous,
            ZoneShare = zoneShare,
            TypicalDay = hours
        });
        typicalDay.Update(hours);

        if (recent is null)
        {
            Bars = buckets.Select(bucket => ToBar(bucket, interval, metric)).ToList();
            WeekendSpans = grouping == Period.Day ? Weekends(start, end) : null;
            Points = null;
            Gaps = EmptyRuns(buckets);
            return;
        }

        WeekendSpans = null;
        Bars = null;
        Points = recent.Select(reading => new ChartPoint(reading.Timestamp, formatter.ToDisplay(metric, reading.ValueOf(metric)))).ToList();
        Gaps = MissingStretches(recent.Select(reading => reading.Timestamp).ToList(), interval);
    }

    /// <summary>Where the period before the one starting at <paramref name="start"/> began: as long as the range (24 hours for Day, else whole local days).</summary>
    private DateTimeOffset StartOfPeriodBefore(Period range, DateTimeOffset start)
        => range == Period.Day ? start - TimeSpan.FromHours(24) : StartOf(range, start.AddTicks(-1));

    private DateTimeOffset StartOf(Period range, DateTimeOffset end)
    {
        if (range == Period.Day)
        {
            return end - TimeSpan.FromHours(24);
        }

        var days = range switch
        {
            Period.Week => 7,
            Period.Month => 30,
            _ => 365
        };

        return calendar.StartOfLocalDay(calendar.LocalDateOf(end).AddDays(1 - days));
    }

    private void Clear()
    {
        IsEmpty = true;
        WeekendSpans = null;
        Bars = [];
        Points = [];
        Gaps = [];
        stats.Update([]);
        typicalDay.Update([]);
    }

    private List<ChartGap> Weekends(DateTimeOffset start, DateTimeOffset end)
    {
        var spans = new List<ChartGap>();

        for (var date = calendar.LocalDateOf(start); calendar.StartOfLocalDay(date) < end; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                spans.Add(new ChartGap(calendar.StartOfLocalDay(date), calendar.StartOfLocalDay(date.AddDays(2))));
            }
        }

        return spans;
    }

    private double? Display(Metric metric, double? stored) => stored is { } value ? formatter.ToDisplay(metric, value) : null;

    private ChartBar ToBar(Bucket bucket, TimeSpan interval, Metric metric)
        => new(bucket.Start, bucket.End, Display(metric, bucket.Minimum), Display(metric, bucket.Maximum), Display(metric, bucket.Average), bucket.State, bucket.ReadingCount * interval.TotalHours, bucket.ExpectedCount * interval.TotalHours);

    private static List<ChartGap> EmptyRuns(IReadOnlyList<Bucket> buckets)
    {
        var runs = new List<ChartGap>();

        foreach (var bucket in buckets.Where(bucket => bucket.State == BucketState.NoData))
        {
            if (runs.Count > 0 && runs[^1].End == bucket.Start)
            {
                runs[^1] = runs[^1] with { End = bucket.End };
            }
            else
            {
                runs.Add(new ChartGap(bucket.Start, bucket.End));
            }
        }

        return runs;
    }

    private static List<ChartGap> MissingStretches(List<DateTimeOffset> times, TimeSpan interval)
    {
        var runs = new List<ChartGap>();

        for (var index = 1; index < times.Count; index++)
        {
            if (times[index] - times[index - 1] > interval * 1.5)
            {
                runs.Add(new ChartGap(times[index - 1] + interval, times[index]));
            }
        }

        return runs;
    }
}
