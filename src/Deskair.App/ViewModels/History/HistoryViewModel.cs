using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.App.ViewModels.Data;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.HoursFilter;
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
    private readonly SleepViewModel sleep;
    private readonly ChartHeaderViewModel chartHeader;
    private readonly ReadingQueryService query;
    private readonly DeviceRegistry devices;
    private readonly LocalCalendar calendar;
    private readonly TimeProvider timeProvider;
    private readonly IMessenger messenger;
    private readonly ILogger<HistoryViewModel> logger;
    private readonly MetricFormatter formatter;
    private readonly PeriodLabeler labeler;
    private readonly HourGridBuilder gridBuilder;
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

    /// <summary>The sleep card (last night) takes the place of the typical day while the range is Day and the hours filter is Night.</summary>
    [ObservableProperty]
    private bool showSleep;

    /// <summary>The days × hours grid in place of the bars (D-0018): CO₂ on Week and Month over all hours. Null otherwise.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGrid))]
    private HourGridModel? grid;

    /// <summary>What Narrator reads for the grid ("CO₂ by hour, 8 – 14 Oct 2026").</summary>
    [ObservableProperty]
    private string? gridName;

    public bool ShowGrid => Grid is not null;

    /// <summary>The room has readings, but none in the chosen period (before the first reading): the screen shows "No readings for …" and "Go to the latest".</summary>
    [ObservableProperty]
    private bool isPeriodEmpty;

    /// <summary>"No readings for 24 – 30 Sep 2026". Null while the period has readings.</summary>
    [ObservableProperty]
    private string? periodEmptyTitle;

    /// <summary>"Deskair has readings from 6 Oct 2026."</summary>
    [ObservableProperty]
    private string? periodEmptyText;

    /// <summary>The average of the whole period in the unit shown, for the dashed line across the main chart. Null when there is none.</summary>
    [ObservableProperty]
    private double? averageLine;

    public HistoryViewModel(
        HistoryToolbarViewModel toolbar,
        StatsViewModel stats,
        TypicalDayViewModel typicalDay,
        SleepViewModel sleep,
        ChartHeaderViewModel chartHeader,
        ExportViewModel export,
        ReadingQueryService query,
        DeviceRegistry devices,
        LocalCalendar calendar,
        TimeProvider timeProvider,
        IMessenger messenger,
        MetricFormatter formatter,
        PeriodLabeler labeler,
        HourGridBuilder gridBuilder,
        ILogger<HistoryViewModel> logger)
    {
        this.toolbar = toolbar;
        this.stats = stats;
        this.typicalDay = typicalDay;
        this.sleep = sleep;
        this.chartHeader = chartHeader;
        Export = export;
        this.query = query;
        this.devices = devices;
        this.calendar = calendar;
        this.timeProvider = timeProvider;
        this.messenger = messenger;
        this.logger = logger;
        this.formatter = formatter;
        this.labeler = labeler;
        this.gridBuilder = gridBuilder;
    }

    public HistoryToolbarViewModel Toolbar => toolbar;

    public StatsViewModel Stats => stats;

    public TypicalDayViewModel TypicalDay => typicalDay;

    public SleepViewModel Sleep => sleep;

    public ChartHeaderViewModel ChartHeader => chartHeader;

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
        toolbar.PropertyChanged += (_, args) =>
        {
            if (HistoryToolbarViewModel.IsChoice(args.PropertyName))
            {
                Refresh();
            }
        };
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
        var now = timeProvider.GetUtcNow();
        var first = await query.GetFirstReadingTimeAsync(device, token);
        var maxOffset = HistoryPeriod.MaxOffset(range, now, first, calendar);

        // A room with less history than before (another room, an import undone) cannot stay further back than it now allows.
        if (toolbar.Offset > maxOffset)
        {
            toolbar.MaxOffset = maxOffset;
            toolbar.Offset = maxOffset;
            return;
        }

        var period = HistoryPeriod.For(range, toolbar.Offset, now, calendar);
        var before = period.Before(now, calendar);
        var (start, end) = (period.Start, period.End);
        var metric = toolbar.Metric;
        var grouping = range == Period.Week ? Period.Day : toolbar.Grouping;
        var interval = devices.IntervalFor(device, end);

        var buckets = await Task.Run(
            () => query.GetBucketsAsync(device, metric, start, end, grouping, toolbar.Hours, token),
            token);
        var hours = await Task.Run(() => query.GetTypicalDayAsync(device, range, start, end, token), token);
        var previous = await Task.Run(
            () => query.GetBucketsAsync(device, metric, before.Start, before.End, grouping, toolbar.Hours, token),
            token);
        var zoneShare = metric == Metric.Co2 ? await query.GetZoneShareAsync(device, start, end, toolbar.Hours, token) : null;
        var recent = range == Period.Day ? await query.GetRangeAsync(device, start, end, token) : null;
        var wantSleep = range == Period.Day && toolbar.Hours == HoursFilterMode.Night;

        // The latest Day's night is last night; a calendar day's is the night that ended that morning (behaviour-rules.md "Sleep" rule 2).
        var lastNight = wantSleep ? await query.GetLastNightAsync(device, metric, period.IsRolling ? now : end, token) : null;

        // D-0018: CO₂ on Week and Month is a grid of days and hours. With the Night filter it stays bars until the filter goes (POL-10).
        var wantGrid = metric == Metric.Co2 && (range is Period.Week or Period.Month) && toolbar.Hours == HoursFilterMode.AllDay;
        var gridDays = wantGrid ? await query.GetHourGridAsync(device, metric, start, end, token) : null;

        token.ThrowIfCancellationRequested();

        IsEmpty = first is null;
        toolbar.MaxOffset = maxOffset;
        toolbar.PeriodLabel = labeler.Label(period);
        IsPeriodEmpty = first is not null && (recent is null ? buckets.All(bucket => bucket.ReadingCount == 0) : recent.Count == 0);
        PeriodEmptyTitle = IsPeriodEmpty ? labeler.EmptyTitle(toolbar.PeriodLabel) : null;
        PeriodEmptyText = IsPeriodEmpty && first is { } since ? labeler.EmptyText(since) : null;
        stats.Update(new StatsInput(buckets)
        {
            Metric = metric,
            Range = range,
            Grouping = grouping,
            Previous = previous,
            ZoneShare = zoneShare,
            Hours = toolbar.Hours,
            TypicalDay = hours
        });
        typicalDay.Update(hours);
        AverageLine = Display(metric, stats.Average);

        ShowSleep = lastNight is not null;
        sleep.Update(lastNight);

        if (recent is null)
        {
            Bars = buckets.Select(bucket => ToBar(bucket, interval, metric)).ToList();
            WeekendSpans = grouping == Period.Day ? Weekends(start, end) : null;
            Points = null;
            Gaps = EmptyRuns(buckets);
            chartHeader.Update(metric, range, grouping, toolbar.Hours, Gaps.Count > 0);
            Grid = gridDays is null ? null : gridBuilder.Build(gridDays, hours, metric, range, query.Night, calendar.LocalDateOf(now));
            GridName = Grid is null ? null : labeler.GridName(metric, toolbar.PeriodLabel);

            if (Grid is not null)
            {
                chartHeader.UpdateForGrid(metric);
            }

            return;
        }

        Grid = null;
        GridName = null;
        WeekendSpans = null;
        Bars = null;
        Points = recent.Select(reading => new ChartPoint(reading.Timestamp, formatter.ToDisplay(metric, reading.ValueOf(metric)))).ToList();
        Gaps = MissingStretches(recent.Select(reading => reading.Timestamp).ToList(), interval);
        chartHeader.Update(metric, range, grouping, toolbar.Hours, Gaps.Count > 0);
    }

    private void Clear()
    {
        IsEmpty = true;
        Grid = null;
        GridName = null;
        IsPeriodEmpty = false;
        PeriodEmptyTitle = null;
        PeriodEmptyText = null;
        toolbar.MaxOffset = 0;
        toolbar.PeriodLabel = labeler.Label(HistoryPeriod.For(toolbar.Range, 0, timeProvider.GetUtcNow(), calendar));
        WeekendSpans = null;
        Bars = [];
        Points = [];
        Gaps = [];
        stats.Update([]);
        typicalDay.Update([]);
        ShowSleep = false;
        AverageLine = null;
        sleep.Update(null);
        chartHeader.Update(toolbar.Metric, toolbar.Range, toolbar.Grouping, toolbar.Hours, hasGaps: false);
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
