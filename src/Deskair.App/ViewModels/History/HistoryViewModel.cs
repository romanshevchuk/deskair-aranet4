using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.App.ViewModels.Data;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.AirImproved;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Queries;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.History;

public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly HistoryToolbarViewModel toolbar;
    private readonly StatsViewModel stats;
    private readonly SleepViewModel sleep;
    private readonly ChartHeaderViewModel chartHeader;
    private readonly ReadingQueryService query;
    private readonly DeviceRegistry devices;
    private readonly LocalCalendar calendar;
    private readonly TimeProvider timeProvider;
    private readonly IMessenger messenger;
    private readonly IUiDispatcher dispatcher;
    private readonly ILogger<HistoryViewModel> logger;
    private readonly MetricFormatter formatter;
    private readonly PeriodLabeler labeler;
    private readonly HourGridBuilder gridBuilder;
    private readonly DayChartBuilder dayChart;
    private CancellationTokenSource? current;
    private bool following;

    [ObservableProperty]
    private IReadOnlyList<ChartPoint>? points;

    [ObservableProperty]
    private IReadOnlyList<ChartGap>? gaps;

    /// <summary>The typical day (the 7 days before) laid over the Day window, in the unit shown. Null outside Day or for other metrics than CO₂.</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartPoint>? typicalPoints;

    /// <summary>The night hours inside the Day window, drawn as a band. Null outside Day.</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartGap>? nightSpans;

    /// <summary>The comfortable range behind the Day line (Temperature, Humidity). Null otherwise.</summary>
    [ObservableProperty]
    private ChartBand? comfortBand;

    /// <summary>The biggest 24 hour change shaded on the Pressure line, with its label. Null otherwise.</summary>
    [ObservableProperty]
    private ChartHighlight? highlight;

    /// <summary>The "air improved" rings on the Day chart (CO₂ only). Null outside Day.</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartMarker>? markers;

    /// <summary>Nothing is stored for the selected room yet, or there is no room: the screen shows "collecting your first data" instead of charts.</summary>
    [ObservableProperty]
    private bool isEmpty;

    /// <summary>The "Last night" section of the summary card (Day only). Not shown for Pressure.</summary>
    [ObservableProperty]
    private bool showSleep;

    /// <summary>The days × hours grid (D-0018): CO₂, Temperature and Humidity on Week, Month and Year. Null for Day and Pressure.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGrid))]
    private HourGridModel? grid;

    /// <summary>What Narrator reads for the grid ("CO₂ by hour, 8 – 14 Oct 2026").</summary>
    [ObservableProperty]
    private string? gridName;

    /// <summary>What Narrator reads for the line chart (Day, and the Pressure line): "Pressure, last 24 hours". Null while the grid is shown.</summary>
    [ObservableProperty]
    private string? chartName;

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

    public HistoryViewModel(
        HistoryToolbarViewModel toolbar,
        StatsViewModel stats,
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
        DayChartBuilder dayChart,
        IUiDispatcher dispatcher,
        ILogger<HistoryViewModel> logger)
    {
        this.toolbar = toolbar;
        this.stats = stats;
        this.sleep = sleep;
        this.chartHeader = chartHeader;
        Export = export;
        this.query = query;
        this.devices = devices;
        this.calendar = calendar;
        this.timeProvider = timeProvider;
        this.messenger = messenger;
        this.dispatcher = dispatcher;
        this.logger = logger;
        this.formatter = formatter;
        this.labeler = labeler;
        this.gridBuilder = gridBuilder;
        this.dayChart = dayChart;
    }

    public HistoryToolbarViewModel Toolbar => toolbar;

    public StatsViewModel Stats => stats;

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
        messenger.Register<HistoryViewModel, SyncCompleted>(this, (self, _) => self.dispatcher.Post(self.Refresh));
        messenger.Register<HistoryViewModel, ReadingsImported>(this, (self, _) => self.dispatcher.Post(self.Refresh));
        messenger.Register<HistoryViewModel, DeviceSelected>(this, (self, _) => self.dispatcher.Post(self.Refresh));
        messenger.Register<HistoryViewModel, SettingsChanged>(this, (self, _) => self.dispatcher.Post(self.Refresh));
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

        var loaded = await ReadAsync(device, first, maxOffset, now, token);

        token.ThrowIfCancellationRequested();

        ShowSummary(loaded);
        ShowChart(loaded);
    }

    /// <summary>Reads everything the screen needs for the toolbar choice. Nothing is shown yet, so a refresh that is replaced half way leaves the screen as it was.</summary>
    private async Task<HistoryLoad> ReadAsync(DeviceId device, DateTimeOffset? first, int maxOffset, DateTimeOffset now, CancellationToken token)
    {
        var range = toolbar.Range;
        var window = HistoryPeriod.For(range, toolbar.Offset, now, calendar);
        var before = window.Before(now, calendar);
        var (start, end) = (window.Start, window.End);
        var metric = toolbar.Metric;
        var grouping = range == Period.Year ? Period.Week : Period.Day;

        var buckets = await Task.Run(
            () => query.GetBucketsAsync(device, metric, start, end, grouping, token),
            token);
        var hours = metric == Metric.Pressure ? [] : await Task.Run(() => query.GetTypicalDayAsync(device, range, start, end, token, metric), token);
        var previous = await Task.Run(
            () => query.GetBucketsAsync(device, metric, before.Start, before.End, grouping, token),
            token);
        var zoneShare = metric == Metric.Co2 ? await query.GetZoneShareAsync(device, start, end, token) : null;
        var comfort = await query.GetComfortShareAsync(device, metric, start, end, token);
        var recent = range == Period.Day ? await query.GetRangeAsync(device, start, end, token) : null;
        var wantSleep = range == Period.Day;

        // The latest Day shows last night; a calendar day shows the night that ended that morning (behaviour-rules.md "Sleep" rule 2).
        var lastNight = wantSleep && metric != Metric.Pressure ? await query.GetLastNightAsync(device, metric, window.IsRolling ? now : end, token) : null;
        var nights = wantSleep || metric == Metric.Pressure ? null : await query.GetNightsAsync(device, metric, start, end, token);

        // D-0018, POL-18: CO₂, Temperature and Humidity on Week and Month are a grid of days and hours, on Year of months and days. Pressure keeps its line.
        var wantGrid = metric is Metric.Co2 or Metric.Temperature or Metric.Humidity;
        // Pressure follows the weather, not the hour: Week and Month are one line of hourly means, Year of daily means.
        var wantLine = metric == Metric.Pressure && range != Period.Day;
        var gridDays = (wantGrid || wantLine) && range is Period.Week or Period.Month ? await query.GetHourGridAsync(device, metric, start, end, token) : null;
        var gridMonths = (wantGrid || wantLine) && range == Period.Year ? await query.GetMonthGridAsync(device, metric, start, end, token) : null;
        var pressure = !wantLine ? null : gridMonths is not null ? dayChart.PressureLine(gridMonths) : dayChart.PressureLine(gridDays ?? []);

        return new HistoryLoad(
            first,
            maxOffset,
            window,
            now,
            metric,
            range,
            grouping,
            devices.IntervalFor(device, end),
            buckets,
            previous,
            hours,
            zoneShare,
            comfort,
            recent,
            lastNight,
            nights,
            gridDays,
            gridMonths,
            pressure);
    }

    /// <summary>The period label, the empty-period message, the stats card and the sleep cards.</summary>
    private void ShowSummary(HistoryLoad loaded)
    {
        IsEmpty = loaded.First is null;
        toolbar.MaxOffset = loaded.MaxOffset;
        toolbar.PeriodLabel = labeler.Label(loaded.Window);
        IsPeriodEmpty = loaded.First is not null && (loaded.Recent is null ? loaded.Buckets.All(bucket => bucket.ReadingCount == 0) : loaded.Recent.Count == 0);
        PeriodEmptyTitle = IsPeriodEmpty ? labeler.EmptyTitle(toolbar.PeriodLabel) : null;
        PeriodEmptyText = IsPeriodEmpty && loaded.First is { } since ? labeler.EmptyText(since) : null;
        stats.Update(new StatsInput(loaded.Buckets)
        {
            Metric = loaded.Metric,
            Range = loaded.Range,
            Grouping = loaded.Grouping,
            Previous = loaded.Previous,
            ZoneShare = loaded.ZoneShare,
            Comfort = loaded.Comfort,
            PressureChange = loaded.Pressure?.Change,
            TypicalDay = loaded.TypicalDay,
            Night = query.Night
        });

        ShowSleep = loaded.LastNight is not null;
        sleep.Update(loaded.LastNight);
        sleep.UpdateNights(loaded.Nights);
    }

    /// <summary>The main card: the Day line, the Pressure line, or the hour or month grid.</summary>
    private void ShowChart(HistoryLoad loaded)
    {
        if (loaded.Recent is { } recent)
        {
            ShowDayLine(loaded, recent);
        }
        else if (loaded.Pressure is { } pressure)
        {
            ShowPressureLine(pressure, loaded.Metric, loaded.Range, loaded.Grouping);
        }
        else
        {
            ShowGridChart(loaded);
        }
    }

    private void ShowGridChart(HistoryLoad loaded)
    {
        var (metric, range, grouping) = (loaded.Metric, loaded.Range, loaded.Grouping);
        Points = null;
        TypicalPoints = null;
        ComfortBand = null;
        Highlight = null;
        NightSpans = null;
        Markers = null;
        Gaps = EmptyRuns(loaded.Buckets);
        chartHeader.Update(metric, range, grouping, Gaps.Count > 0);
        var today = calendar.LocalDateOf(loaded.Now);
        var byDay = loaded.GridMonths is not null;

        Grid = loaded.GridMonths is not null
            ? gridBuilder.BuildYear(loaded.GridMonths, metric, today)
            : loaded.GridDays is null ? null : gridBuilder.Build(loaded.GridDays, loaded.TypicalDay, metric, range, query.Night, today);
        GridName = Grid is null ? null : labeler.GridName(metric, toolbar.PeriodLabel ?? string.Empty, byDay);
        ChartName = null;

        if (Grid is not null)
        {
            chartHeader.UpdateForGrid(metric, byDay);
        }
    }

    private void ShowDayLine(HistoryLoad loaded, IReadOnlyList<Reading> recent)
    {
        var (metric, range, grouping) = (loaded.Metric, loaded.Range, loaded.Grouping);
        var (start, end) = (loaded.Window.Start, loaded.Window.End);
        Grid = null;
        GridName = null;
        ChartName = labeler.ChartName(metric, toolbar.PeriodLabel ?? string.Empty);
        Points = recent.Select(reading => new ChartPoint(reading.Timestamp, formatter.ToDisplay(metric, reading.ValueOf(metric)))).ToList();
        NightSpans = metric == Metric.Pressure ? null : dayChart.NightSpans(query.Night, start, end);
        Highlight = null;
        TypicalPoints = metric == Metric.Pressure ? null : dayChart.TypicalLine(loaded.TypicalDay, start, end, metric);
        ComfortBand = dayChart.ComfortBand(metric);
        Markers = metric == Metric.Co2 ? dayChart.Markers(AirImprovedDetector.Detect([.. recent.OrderBy(reading => reading.Timestamp)])) : null;
        Gaps = MissingStretches(recent.Select(reading => reading.Timestamp).ToList(), loaded.Interval);
        chartHeader.Update(metric, range, grouping, Gaps.Count > 0);
    }

    private void ShowPressureLine(PressureSeries pressure, Metric metric, Period range, Period grouping)
    {
        Grid = null;
        GridName = null;
        ChartName = labeler.ChartName(metric, toolbar.PeriodLabel ?? string.Empty);
        Points = pressure.Points;
        TypicalPoints = null;
        ComfortBand = null;
        NightSpans = null;
        Markers = null;
        Highlight = dayChart.ChangeHighlight(pressure.Change);
        Gaps = MissingStretches([.. pressure.Points.Select(point => point.Time)], pressure.Interval);
        chartHeader.Update(metric, range, grouping, Gaps.Count > 0);
        chartHeader.UpdateForLine(byDay: range == Period.Year);
    }

    private void Clear()
    {
        IsEmpty = true;
        Grid = null;
        GridName = null;
        ChartName = null;
        IsPeriodEmpty = false;
        PeriodEmptyTitle = null;
        PeriodEmptyText = null;
        toolbar.MaxOffset = 0;
        toolbar.PeriodLabel = labeler.Label(HistoryPeriod.For(toolbar.Range, 0, timeProvider.GetUtcNow(), calendar));
        Highlight = null;
        Points = [];
        Gaps = [];
        stats.Update([]);
        ShowSleep = false;
        sleep.Update(null);
        sleep.UpdateNights(null);
        chartHeader.Update(toolbar.Metric, toolbar.Range, toolbar.Range == Period.Year ? Period.Week : Period.Day, hasGaps: false);
    }

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
