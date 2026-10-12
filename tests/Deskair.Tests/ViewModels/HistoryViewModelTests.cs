using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Queries;
using Deskair.Services.Storage;
using Deskair.Tests.Fakes;

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
    private readonly StatsViewModel stats = new();
    private readonly TypicalDayViewModel typicalDay = new();

    public HistoryViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
    }

    private HistoryViewModel Create(IReadingStore? readingStore = null)
    {
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);
        var clock = new FakeClock(now);
        var query = new ReadingQueryService(readingStore ?? store, registry, clock, calendar);

        return new HistoryViewModel(toolbar, stats, typicalDay, query, registry, calendar, clock);
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
}
