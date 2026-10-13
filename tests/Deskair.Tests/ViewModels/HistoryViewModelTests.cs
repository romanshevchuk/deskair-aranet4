using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Queries;
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
    private readonly TypicalDayViewModel typicalDay = new();
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

        return new HistoryViewModel(toolbar, stats, typicalDay, sleep, TestExport.Create(), query, registry, calendar, clock, messenger, TestFormatter.Create(new FakeLocalizer()), NullLogger<HistoryViewModel>.Instance);
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

        Assert.Empty(viewModel.Bars!);
        Assert.Empty(viewModel.Points!);
        Assert.Empty(viewModel.Gaps!);
        Assert.Null(stats.Average);
        Assert.True(viewModel.IsEmpty);
    }

    [Fact]
    public async Task Week_gives_one_bar_per_local_day_ending_today()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 10), 800);
        AddDay(Samples.Utc(2026, 10, 11), 1200);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(7, viewModel.Bars!.Count);
        Assert.Equal(Samples.Utc(2026, 10, 6), viewModel.Bars[0].Start);
        Assert.Equal(now, viewModel.Bars[^1].End);
        Assert.Equal(1200, viewModel.Bars[5].Average);
        Assert.Null(viewModel.Points);
    }

    [Fact]
    public async Task Month_uses_the_selected_grouping()
    {
        await SelectDeviceAsync();
        var viewModel = Create();
        toolbar.Range = Period.Month;
        toolbar.Grouping = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, viewModel.Bars!.Count);
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
        Assert.Null(viewModel.Bars);
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
    public async Task Refresh_fills_the_stats_and_the_typical_day()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11), 1000);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1000, stats.Average);
        Assert.Equal(24, typicalDay.Bars.Count);
        Assert.Equal(1000, typicalDay.Bars[3].Average);
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
        Assert.Equal(7, viewModel.Bars!.Count);
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
    public async Task Bars_for_single_days_get_the_weekend_spans_behind_them()
    {
        await SelectDeviceAsync();
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // 12 Oct 2026 is a Monday, so the week of 6 to 12 Oct has one weekend: Saturday 10 to Monday 12.
        var weekend = Assert.Single(viewModel.WeekendSpans!);
        Assert.Equal((Samples.Utc(2026, 10, 10), Samples.Utc(2026, 10, 12)), (weekend.Start, weekend.End));
    }

    [Fact]
    public async Task A_month_by_day_has_four_weekends_and_by_week_none()
    {
        await SelectDeviceAsync();
        var viewModel = Create();
        toolbar.Range = Period.Month;
        toolbar.Grouping = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(4, viewModel.WeekendSpans!.Count);

        toolbar.Grouping = Period.Week;
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Null(viewModel.WeekendSpans);
    }

    [Fact]
    public async Task The_day_line_has_no_weekend_spans()
    {
        await SelectDeviceAsync();
        var viewModel = Create();
        toolbar.Range = Period.Day;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Null(viewModel.WeekendSpans);
    }

    [Fact]
    public async Task A_bar_knows_how_many_hours_were_recorded_of_how_many_expected()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11), 800, hours: 6);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        var bar = viewModel.Bars![5];
        Assert.Equal(6, bar.RecordedHours, 3);
        Assert.Equal(24, bar.ExpectedHours, 3);
    }

    [Fact]
    public async Task Refresh_loads_the_screen_in_the_background()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11), 1200);
        var viewModel = Create();
        toolbar.Range = Period.Week;

        viewModel.Refresh();

        await Wait.Until(() => viewModel.Bars is { Count: 7 });
        Assert.Same(toolbar, viewModel.Toolbar);
        Assert.Same(stats, viewModel.Stats);
        Assert.Same(typicalDay, viewModel.TypicalDay);
    }

    [Fact]
    public async Task A_changed_toolbar_choice_loads_the_screen_again()
    {
        await SelectDeviceAsync();
        var viewModel = Create();
        toolbar.Range = Period.Week;
        viewModel.Refresh();
        await Wait.Until(() => viewModel.Bars is { Count: 7 });

        toolbar.Range = Period.Month;
        toolbar.Grouping = Period.Week;

        await Wait.Until(() => viewModel.Bars is { Count: 5 });
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
        await Wait.Until(() => viewModel.Bars is { Count: 7 });
        AddDay(Samples.Utc(2026, 10, 11), 1500);

        if (imported)
        {
            messenger.Send(new ReadingsImported(device, 720));
        }
        else
        {
            messenger.Send(new SyncCompleted(device, 100));
        }

        await Wait.Until(() => viewModel.Bars![5].Average == 1500);
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
    public async Task Zone_share_is_loaded_for_CO2_and_left_out_for_other_metrics()
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
    public async Task Day_with_the_night_filter_shows_the_sleep_card_for_last_night()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11, 22), 700, hours: 9);
        var viewModel = Create();
        toolbar.Range = Period.Day;
        toolbar.Hours = HoursFilterMode.Night;

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
        toolbar.Hours = HoursFilterMode.Night;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.ShowSleep);
        Assert.False(sleep.HasData);
        Assert.Equal("Sleep_Empty", sleep.Tip);
    }

    [Theory]
    [InlineData(Period.Day, HoursFilterMode.AllDay)]
    [InlineData(Period.Week, HoursFilterMode.Night)]
    [InlineData(Period.Month, HoursFilterMode.Night)]
    public async Task The_sleep_card_is_only_for_the_day_with_the_night_filter(Period range, HoursFilterMode hours)
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11, 22), 700, hours: 9);
        var viewModel = Create();
        toolbar.Range = range;
        toolbar.Hours = hours;

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShowSleep);
        Assert.False(sleep.HasData);
    }

    [Fact]
    public async Task Leaving_the_night_filter_takes_the_card_away_again()
    {
        await SelectDeviceAsync();
        AddDay(Samples.Utc(2026, 10, 11, 22), 700, hours: 9);
        var viewModel = Create();
        toolbar.Range = Period.Day;
        toolbar.Hours = HoursFilterMode.Night;
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        toolbar.Hours = HoursFilterMode.AllDay;
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShowSleep);
    }

    [Fact]
    public async Task Without_a_selected_device_there_is_no_sleep_card()
    {
        var viewModel = Create();
        toolbar.Range = Period.Day;
        toolbar.Hours = HoursFilterMode.Night;

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
        var viewModel = new HistoryViewModel(toolbar, stats, typicalDay, sleep, TestExport.Create(), query, registry, calendar, clock, messenger, TestFormatter.Create(new FakeLocalizer()), logger);

        viewModel.Refresh();

        await Wait.Until(() => logger.Entries.Any(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning));
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

        public Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
            => Task.FromResult<DateTimeOffset?>(null);

        public Task<int> MergeAsync(DeviceId device, IReadOnlyList<Reading> readings, ReadingSource source, TimeSpan interval, CancellationToken cancellationToken)
            => Task.FromResult(0);
    }
}
