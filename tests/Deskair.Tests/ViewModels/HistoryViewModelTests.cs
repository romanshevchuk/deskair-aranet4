using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.ChartModels;
using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Queries;
using Deskair.Services.Settings;
using Deskair.Services.Storage;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.ViewModels;

public class HistoryViewModelTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);
    private static readonly DeviceId device = new("test", "A1");
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);

    private readonly FakeReadingStore store = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly DeviceRegistry registry;
    private readonly HistoryToolbarViewModel toolbar = new();
    private readonly StatsViewModel stats;
    private readonly SleepViewModel sleep;
    private readonly StrongReferenceMessenger messenger = new();

    public HistoryViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        var localizer = new FakeLocalizer();
        stats = new StatsViewModel(TestFormatter.Create(localizer), localizer, new LocalCalendar(TimeZoneInfo.Utc));
        sleep = new SleepViewModel(localizer, TestFormatter.Create(localizer), new LocalCalendar(TimeZoneInfo.Utc));
    }

    private HistoryViewModel Create(IReadingStore? readingStore = null)
    {
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);
        var clock = new FakeClock(now);
        var query = new ReadingQueryService(readingStore ?? store, registry, clock, calendar);

        return new HistoryViewModel(toolbar, stats, sleep, new ChartHeaderViewModel(new FakeLocalizer()), TestExport.Create(), query, registry, calendar, clock, messenger, TestFormatter.Create(new FakeLocalizer()), new PeriodLabeler(new FakeLocalizer(), calendar), new HourGridBuilder(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer())), new DayChartBuilder(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer()), calendar), new FakeUiDispatcher(), NullLogger<HistoryViewModel>.Instance);
    }

    private async Task SelectDeviceAsync()
    {
        deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [new IntervalChange(now.AddYears(-1), interval)], null));
        await registry.LoadAsync(TestContext.Current.CancellationToken);
    }

    private void AddDay(DateTimeOffset dayStart, double ppm, int hours = 24)
    {
        for (var slot = 0; slot < hours * 30; slot++)
        {
            store.Readings.Add(Samples.Co2(dayStart.AddMinutes(slot * 2), ppm));
        }
    }

    [Fact]
    public async Task No_selected_device_clears_everything()
    {
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Null(viewModel.Grid);
        Assert.Empty(viewModel.Points!);
        Assert.Empty(viewModel.Gaps!);
        Assert.Null(stats.Average);
        Assert.True(viewModel.IsEmpty);
    }

    [Fact]
    public async Task The_line_chart_is_named_by_metric_and_period_and_the_grid_has_no_chart_name()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 5), 1010);
        var viewModel = Create();
        toolbar.Metric = Metric.Pressure;
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.StartsWith("History_Chart_Name(metric=Metric_Pressure", viewModel.ChartName, StringComparison.Ordinal);
        Assert.Null(viewModel.GridName);
    }
    [Fact]
    public async Task Year_for_co2_is_a_grid_of_months_and_days_named_by_day()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 5), 800);
        var viewModel = Create();
        toolbar.Range = Period.Year;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(viewModel.Grid);
        Assert.Equal(31, viewModel.Grid.Columns);
        Assert.Null(viewModel.Grid.Typical);
        Assert.True(viewModel.ShowGrid);
        Assert.StartsWith("History_Grid_NameDay(", viewModel.GridName, StringComparison.Ordinal);
        Assert.Null(viewModel.ChartName);
        Assert.Equal("Oct 2026", viewModel.Grid.Rows[^1].Label);
        Assert.Equal(800, viewModel.Grid.Rows[^1].Cells[4]!.Value);
    }

    [Fact]
    public async Task Day_for_co2_adds_the_night_band_the_typical_line_and_the_air_improved_markers()
    {
        await SelectDeviceAsync();
        AddDay(now.AddHours(-30), 700);
        store.Readings.Add(Samples.Co2(now.AddHours(-3), 1500));
        store.Readings.Add(Samples.Co2(now.AddHours(-3).AddMinutes(2), 1380));
        store.Readings.Add(Samples.Co2(now.AddHours(-3).AddMinutes(4), 1200));
        store.Readings.Add(Samples.Co2(now.AddHours(-3).AddMinutes(6), 1190));
        var viewModel = Create();
        toolbar.Range = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(viewModel.NightSpans!);
        Assert.NotEmpty(viewModel.TypicalPoints!);
        var marker = Assert.Single(viewModel.Markers!);
        Assert.Equal(now.AddHours(-3), marker.Time);
    }

    [Fact]
    public async Task Day_for_humidity_has_the_night_band_the_typical_line_and_the_comfort_band_but_no_markers()
    {
        await SelectDeviceAsync();
        AddDay(now.AddHours(-30), 700);
        var viewModel = Create();
        toolbar.Range = Period.Day;
        toolbar.Metric = Metric.Humidity;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(viewModel.NightSpans!);
        Assert.NotEmpty(viewModel.TypicalPoints!);
        Assert.Equal(45, viewModel.TypicalPoints![0].Value);
        Assert.NotNull(viewModel.ComfortBand);
        Assert.Equal((30, 50), (viewModel.ComfortBand.Low, viewModel.ComfortBand.High));
        Assert.Null(viewModel.Markers);
    }

    [Fact]
    public async Task Day_for_pressure_has_no_night_band_typical_line_comfort_band_or_markers()
    {
        await SelectDeviceAsync();
        AddDay(now.AddHours(-30), 700);
        var viewModel = Create();
        toolbar.Range = Period.Day;
        toolbar.Metric = Metric.Pressure;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Null(viewModel.NightSpans);
        Assert.Null(viewModel.TypicalPoints);
        Assert.Null(viewModel.ComfortBand);
        Assert.Null(viewModel.Markers);
    }

    [Fact]
    public async Task Week_has_no_day_chart_extras()
    {
        await SelectDeviceAsync();
        AddDay(now.AddHours(-30), 700);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Null(viewModel.NightSpans);
        Assert.Null(viewModel.TypicalPoints);
        Assert.Null(viewModel.Markers);
    }

    [Theory]
    [InlineData(Period.Week, Metric.Humidity)]
    [InlineData(Period.Month, Metric.Humidity)]
    [InlineData(Period.Month, Metric.Temperature)]
    public async Task The_grid_header_names_the_hour_and_has_no_bar_legend(Period range, Metric metric)
    {
        await SelectDeviceAsync();
        AddDay(now.AddHours(-30), 700);
        var viewModel = Create();
        toolbar.Range = range;
        toolbar.Metric = metric;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(viewModel.Grid);
        Assert.Equal("History_Chart_ByHour", viewModel.ChartHeader.Subtitle);
        Assert.Equal(string.Empty, viewModel.ChartHeader.LegendText);
    }

    [Fact]
    public async Task Year_for_pressure_keeps_the_bars()
    {
        await SelectDeviceAsync();
        var viewModel = Create();
        toolbar.Range = Period.Year;
        toolbar.Metric = Metric.Pressure;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Null(viewModel.Grid);
    }

    [Theory]
    [InlineData(Metric.Temperature, HourGridPalette.Temperature)]
    [InlineData(Metric.Humidity, HourGridPalette.Humidity)]
    public async Task Year_for_temperature_and_humidity_is_a_grid_in_the_comfort_palette(Metric metric, HourGridPalette palette)
    {
        await SelectDeviceAsync();
        var viewModel = Create();
        toolbar.Range = Period.Year;
        toolbar.Metric = metric;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(viewModel.Grid);
        Assert.Equal(palette, viewModel.Grid.Palette);
    }

    [Fact]
    public async Task Day_gives_a_line_of_the_selected_metric_and_no_bars()
    {
        await SelectDeviceAsync();
        store.Readings.Add(Samples.Climate(now.AddMinutes(-4), 21.5, 40, 1000));
        store.Readings.Add(Samples.Climate(now.AddMinutes(-2), 22.5, 41, 1001));
        var viewModel = Create();
        toolbar.Range = Period.Day;
        toolbar.Metric = Metric.Temperature;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal([21.5, 22.5], viewModel.Points!.Select(point => point.Value));
    }

    [Fact]
    public async Task Day_reports_a_missing_stretch_as_a_gap()
    {
        await SelectDeviceAsync();
        store.Readings.Add(Samples.Co2(now.AddMinutes(-30), 700));
        store.Readings.Add(Samples.Co2(now.AddMinutes(-10), 900));
        var viewModel = Create();
        toolbar.Range = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        var gap = Assert.Single(viewModel.Gaps!);
        Assert.Equal(now.AddMinutes(-28), gap.Start);
        Assert.Equal(now.AddMinutes(-10), gap.End);
    }

    [Fact]
    public async Task Neighbouring_empty_buckets_merge_into_one_gap()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 6), 800);
        AddDay(Samples.Utc(2026, 10, 10), 800);
        AddDay(Samples.Utc(2026, 10, 11), 800);
        AddDay(Samples.Utc(2026, 10, 12), 800, 12);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        var gap = Assert.Single(viewModel.Gaps!);
        Assert.Equal(Samples.Utc(2026, 10, 7), gap.Start);
        Assert.Equal(Samples.Utc(2026, 10, 10), gap.End);
    }

    [Fact]
    public async Task Refresh_fills_the_stats()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11), 1000);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1000, stats.Average);
    }

    [Fact]
    public async Task A_newer_refresh_cancels_the_previous_one()
    {
        await SelectDeviceAsync();
        var gated = new GatedReadingStore(store);
        var viewModel = Create(gated);
        toolbar.Range = Period.Week;

        var first = viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        await gated.FirstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        await first.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.True(first.IsCompletedSuccessfully);
        Assert.True(viewModel.ShowGrid);
    }

    [Fact]
    public async Task Cancelling_the_caller_token_throws()
    {
        await SelectDeviceAsync();
        var viewModel = Create(new GatedReadingStore(store));
        using var cts = new CancellationTokenSource();
        toolbar.Range = Period.Week;

        var refresh = viewModel.RefreshAsync(cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh);
    }

    [Fact]
    public async Task Refresh_loads_the_screen_in_the_background()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11), 1200);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        viewModel.Refresh();

        await Wait.Until(() => viewModel.ShowGrid);
        Assert.Same(toolbar, viewModel.Toolbar);
        Assert.Same(stats, viewModel.Stats);
    }

    [Fact]
    public async Task A_changed_toolbar_choice_loads_the_screen_again()
    {
        await SelectDeviceAsync();
        var viewModel = Create();
        toolbar.Range = Period.Week;
        viewModel.Refresh();
        await Wait.Until(() => viewModel.ShowGrid);

        toolbar.Range = Period.Month;

        await Wait.Until(() => stats.HeaderText is { } header && header.Contains("13 Sep", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task New_readings_from_a_sync_or_an_import_load_the_screen_again(bool imported)
    {
        await SelectDeviceAsync();
        var viewModel = Create();
        toolbar.Range = Period.Week;
        viewModel.Refresh();
        await Wait.Until(() => viewModel.ShowGrid);
        AddDay(Samples.Utc(2026, 10, 11), 1500);

        if (imported)
        {
            messenger.Send(new ReadingsImported(device, 720));
        }
        else
        {
            messenger.Send(new SyncCompleted(device, 100));
        }

        await Wait.Until(() => stats.Average == 1500);
    }

    [Fact]
    public async Task The_week_is_compared_with_the_week_before()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 3), 1000);
        AddDay(Samples.Utc(2026, 10, 10), 800);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal("▼ 200 ppm", stats.ChangeText);
        Assert.Equal("Stats_Change_Week", stats.ChangeCaption);
    }

    [Fact]
    public async Task The_day_is_compared_with_the_24_hours_before()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 10, 12), 1000);
        AddDay(Samples.Utc(2026, 10, 11, 12), 1400, hours: 24);
        var viewModel = Create();
        toolbar.Range = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal("▲ 400 ppm", stats.ChangeText);
    }

    [Fact]
    public async Task The_share_is_zones_for_CO2_the_comfort_range_for_temperature_and_left_out_for_pressure()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 10), 800);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.True(stats.HasZoneShare);
        Assert.Equal(1, stats.ZoneGood);

        toolbar.Metric = Metric.Temperature;
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.True(stats.HasZoneShare);
        Assert.Equal(HourGridPalette.Temperature, stats.SharePalette);
        Assert.Equal(1, stats.ZoneElevated);

        toolbar.Metric = Metric.Pressure;
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.False(stats.HasZoneShare);
    }

    [Fact]
    public async Task The_insight_comes_from_the_typical_day()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 10), 800);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.StartsWith("Insight_AllGood", stats.InsightText);
    }

    [Fact]
    public async Task The_screen_is_empty_until_readings_are_stored()
    {
        await SelectDeviceAsync();
        var viewModel = Create();

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.True(viewModel.IsEmpty);

        AddDay(Samples.Utc(2026, 10, 11), 800);
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsEmpty);
    }

    [Fact]
    public void Asking_to_import_tells_the_app()
    {
        var viewModel = Create();
        var requests = 0;
        messenger.Register<ImportRequested>(this, (_, _) => requests++);

        viewModel.ImportCommand.Execute(null);

        Assert.Equal(1, requests);
    }


    [Fact]
    public async Task Day_shows_last_night_in_the_summary()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11, 22), 700, hours: 9);
        var viewModel = Create();
        toolbar.Range = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.ShowSleep);
        Assert.True(sleep.HasData);
        Assert.Equal("Sleep_Verdict_Good", sleep.VerdictText);
        Assert.Equal("Sleep_Title(from=22:00, to=07:00)", sleep.Title);
    }

    [Fact]
    public async Task A_night_without_readings_still_shows_the_card_and_says_so()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 10), 700, hours: 8);
        var viewModel = Create();
        toolbar.Range = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.ShowSleep);
        Assert.False(sleep.HasData);
        Assert.Equal("Sleep_Empty", sleep.Tip);
    }

    [Theory]
    [InlineData(Period.Week)]
    [InlineData(Period.Month)]
    [InlineData(Period.Year)]
    public async Task The_last_night_section_is_only_for_the_day(Period range)
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11, 22), 700, hours: 9);
        var viewModel = Create();
        toolbar.Range = range;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShowSleep);
        Assert.False(sleep.HasData);
    }


    [Fact]
    public async Task Pressure_has_no_last_night_section_on_day()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11, 22), 700, hours: 9);
        var viewModel = Create();
        toolbar.Range = Period.Day;
        toolbar.Metric = Metric.Pressure;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShowSleep);
        Assert.False(sleep.HasData);
    }

    [Fact]
    public async Task Co2_keeps_the_last_night_section_on_day()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11, 22), 700, hours: 9);
        var viewModel = Create();
        toolbar.Range = Period.Day;
        toolbar.Metric = Metric.Co2;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.ShowSleep);
        Assert.True(sleep.HasData);
    }

    [Fact]
    public async Task With_no_night_hours_to_show_the_typical_day_stays()
    {
        await SelectDeviceAsync();
        var settingsStore = new FakeSettingsStore { Stored = AppSettings.Default with { NightStart = new TimeOnly(7, 0), NightEnd = new TimeOnly(7, 0) } };
        var settings = new SettingsService(settingsStore, messenger);
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);
        var clock = new FakeClock(now);
        var query = new ReadingQueryService(store, registry, clock, calendar, settings);
        var viewModel = new HistoryViewModel(toolbar, stats, sleep, new ChartHeaderViewModel(new FakeLocalizer()), TestExport.Create(), query, registry, calendar, clock, messenger, TestFormatter.Create(new FakeLocalizer()), new PeriodLabeler(new FakeLocalizer(), calendar), new HourGridBuilder(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer())), new DayChartBuilder(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer()), calendar), new FakeUiDispatcher(), NullLogger<HistoryViewModel>.Instance);
        toolbar.Range = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShowSleep);
    }

    [Fact]
    public async Task Without_a_selected_device_there_is_no_sleep_card()
    {
        var viewModel = Create();
        toolbar.Range = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShowSleep);
    }
    [Fact]
    public async Task A_failing_load_is_logged_not_thrown()
    {
        await SelectDeviceAsync();
        var logger = new ListLogger<HistoryViewModel>();
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);
        var clock = new FakeClock(now);
        var query = new ReadingQueryService(new ThrowingReadingStore(), registry, clock, calendar);
        var viewModel = new HistoryViewModel(toolbar, stats, sleep, new ChartHeaderViewModel(new FakeLocalizer()), TestExport.Create(), query, registry, calendar, clock, messenger, TestFormatter.Create(new FakeLocalizer()), new PeriodLabeler(new FakeLocalizer(), calendar), new HourGridBuilder(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer())), new DayChartBuilder(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer()), calendar), new FakeUiDispatcher(), logger);

        viewModel.Refresh();

        await Wait.Until(() => logger.Entries.Any(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning));
    }

    [Fact]
    public async Task Going_back_a_day_shows_the_calendar_day_before_with_its_title()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 10), 800);
        AddDay(Samples.Utc(2026, 10, 11), 1200);
        var viewModel = Create();
        toolbar.Range = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal("History_Period_Last24Hours", toolbar.PeriodLabel);
        Assert.True(toolbar.PreviousPeriodCommand.CanExecute(null));
        Assert.False(toolbar.NextPeriodCommand.CanExecute(null));

        toolbar.PreviousPeriodCommand.Execute(null);
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // D-0019: before the last 24 hours come calendar days; 11 October 2026 is a Sunday.
        Assert.Equal("Sun 11 Oct", toolbar.PeriodLabel);
        Assert.NotEmpty(viewModel.Points!);
        Assert.All(viewModel.Points!, point => Assert.Equal(1200, point.Value));
        Assert.True(toolbar.NextPeriodCommand.CanExecute(null));
    }

    [Fact]
    public async Task Back_stops_one_period_before_the_first_reading_which_shows_the_empty_state()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 10), 800);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // The week of 6 to 12 October holds the first reading, so ‹ reaches one week further: 29 September to 5 October.
        Assert.Equal(1, toolbar.MaxOffset);
        Assert.False(viewModel.IsPeriodEmpty);

        toolbar.PreviousPeriodCommand.Execute(null);
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsPeriodEmpty);
        Assert.Equal("History_Period_Span(from=29 Sep, to=5 Oct 2026)", toolbar.PeriodLabel);
        Assert.Equal("History_Period_Empty_Title(period=History_Period_Span(from=29 Sep, to=5 Oct 2026))", viewModel.PeriodEmptyTitle);
        Assert.Equal("History_Period_Empty_Text(date=10 Oct 2026)", viewModel.PeriodEmptyText);
        Assert.False(toolbar.PreviousPeriodCommand.CanExecute(null));

        toolbar.LatestCommand.Execute(null);

        Assert.Equal(0, toolbar.Offset);
    }

    [Fact]
    public void Changing_the_range_returns_to_the_latest_period()
    {
        toolbar.Range = Period.Week;
        toolbar.Offset = 3;

        toolbar.Range = Period.Month;

        Assert.Equal(0, toolbar.Offset);
    }

    [Theory]
    [InlineData(nameof(HistoryToolbarViewModel.Offset), true)]
    [InlineData(nameof(HistoryToolbarViewModel.Range), true)]
    [InlineData(nameof(HistoryToolbarViewModel.PeriodLabel), false)]
    [InlineData(nameof(HistoryToolbarViewModel.MaxOffset), false)]
    public void Only_a_choice_reloads_the_screen_not_the_title_or_the_limit_it_sets(string property, bool reloads)
    {
        Assert.Equal(reloads, HistoryToolbarViewModel.IsChoice(property));
    }

    private sealed class GatedReadingStore : IReadingStore
    {
        private readonly IReadingStore inner;
        private int calls;

        public GatedReadingStore(IReadingStore inner)
        {
            this.inner = inner;
        }

        public TaskCompletionSource FirstCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IReadOnlyList<Reading>> ReadRangeAsync(
            DeviceId device,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                FirstCallStarted.SetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return await inner.ReadRangeAsync(device, start, end, cancellationToken);
        }

        public Task<DateTimeOffset?> GetFirstReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
            => inner.GetFirstReadingTimeAsync(device, cancellationToken);

        public Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
            => inner.GetLastReadingTimeAsync(device, cancellationToken);

        public Task<int> MergeAsync(
            DeviceId device,
            IReadOnlyList<Reading> readings,
            ReadingSource source,
            TimeSpan interval,
            CancellationToken cancellationToken)
            => inner.MergeAsync(device, readings, source, interval, cancellationToken);
    }

    private sealed class ThrowingReadingStore : IReadingStore
    {
        public Task<IReadOnlyList<Reading>> ReadRangeAsync(DeviceId device, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
            => throw new InvalidOperationException("disk");

        public Task<DateTimeOffset?> GetFirstReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
            => Task.FromResult<DateTimeOffset?>(null);

        public Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
            => Task.FromResult<DateTimeOffset?>(null);

        public Task<int> MergeAsync(DeviceId device, IReadOnlyList<Reading> readings, ReadingSource source, TimeSpan interval, CancellationToken cancellationToken)
            => Task.FromResult(0);
    }
}
