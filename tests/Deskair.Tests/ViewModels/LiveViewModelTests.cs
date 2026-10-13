using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Live;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Queries;
using Deskair.Services.Settings;
using Deskair.Services.Storage;
using Deskair.Services.Sync;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace Deskair.Tests.ViewModels;

public class LiveViewModelTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);
    private static readonly DeviceId device = new("test", "A1");

    private readonly FakeReadingStore store = new();
    private readonly FlakyStore flaky;
    private readonly ListLogger<LiveViewModel> logger = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly DeviceRegistry registry;
    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeUiDispatcher dispatcher = new();
    private readonly HeroViewModel hero = TestHero.Create();
    private readonly GaugeViewModel gauge = new();
    private readonly MetricTabsViewModel tabs = new();
    private readonly MiniCardsViewModel miniCards = new(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer()));
    private readonly ChartCardViewModel chartCard;
    private readonly LiveViewModel viewModel;

    public LiveViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        var clock = new FakeClock(now);
        flaky = new FlakyStore(store);
        var query = new ReadingQueryService(flaky, registry, clock, new LocalCalendar(TimeZoneInfo.Utc));
        chartCard = new ChartCardViewModel(query, registry, TestFormatter.Create(new FakeLocalizer()), new FakeLocalizer(), clock);
        viewModel = new LiveViewModel(
            hero,
            gauge,
            tabs,
            chartCard,
            miniCards,
            query,
            registry,
            messenger,
            dispatcher,
            clock,
            logger);
    }

    private async Task SelectDeviceAsync()
    {
        deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [new IntervalChange(now.AddYears(-1), TimeSpan.FromMinutes(2))], null));
        await registry.LoadAsync(TestContext.Current.CancellationToken);
    }

    private void AddHour(double ppm)
    {
        for (var minute = 60; minute >= 2; minute -= 2)
        {
            store.Readings.Add(Samples.Co2(now.AddMinutes(-minute), ppm));
        }
    }

    [Fact]
    public async Task Start_loads_every_child()
    {
        await SelectDeviceAsync();
        AddHour(1100);

        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1100, hero.Co2Ppm);
        Assert.Equal(Zone.Elevated, hero.Zone);
        Assert.Equal(1100, gauge.Value);
        Assert.Equal(3, miniCards.Items.Count);
        Assert.NotEmpty(chartCard.Points!);
    }

    [Fact]
    public async Task Start_without_a_device_leaves_the_children_empty()
    {
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        Assert.Null(hero.Co2Ppm);
        Assert.Null(gauge.Value);
        Assert.Empty(chartCard.Points!);
    }

    [Fact]
    public async Task A_new_reading_for_the_selected_device_updates_hero_and_gauge()
    {
        await SelectDeviceAsync();
        AddHour(600);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        messenger.Send(new ReadingReceived(device, Samples.Co2(now, 1500)));

        Assert.Equal(1500, hero.Co2Ppm);
        Assert.Equal(Zone.High, hero.Zone);
        Assert.Equal(1500, gauge.Value);
    }

    [Fact]
    public async Task A_reading_from_another_device_is_ignored()
    {
        await SelectDeviceAsync();
        AddHour(600);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        messenger.Send(new ReadingReceived(new DeviceId("test", "other"), Samples.Co2(now, 1500)));

        Assert.Equal(600, hero.Co2Ppm);
    }

    [Fact]
    public async Task A_zone_change_keeps_the_hero_in_step()
    {
        await SelectDeviceAsync();
        AddHour(600);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        var posted = dispatcher.Posted;

        messenger.Send(new ZoneChanged(device, Zone.Good, Zone.Elevated, now));

        Assert.Equal(posted + 1, dispatcher.Posted);
        Assert.Equal(600, hero.Co2Ppm);
    }

    [Fact]
    public async Task Selecting_another_metric_reloads_the_chart_and_the_mini_cards()
    {
        await SelectDeviceAsync();
        store.Readings.Add(Samples.Climate(now.AddMinutes(-4), 21.5, 40, 1000));
        store.Readings.Add(Samples.Climate(now.AddMinutes(-2), 22.5, 41, 1001));
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        tabs.Select(Metric.Temperature, LiveRange.OneHour);

        await Wait.Until(() => chartCard.Points!.Any(point => point.Value == 22.5));
        Assert.DoesNotContain(miniCards.Items, item => item.Metric == Metric.Temperature);
    }

    [Fact]
    public async Task Cancelling_stops_all_updates()
    {
        await SelectDeviceAsync();
        AddHour(600);
        using var cts = new CancellationTokenSource();
        await viewModel.StartAsync(cts.Token);

        await cts.CancelAsync();
        messenger.Send(new ReadingReceived(device, Samples.Co2(now, 1500)));
        var posted = dispatcher.Posted;
        tabs.Select(Metric.Humidity, LiveRange.SixHours);

        Assert.Equal(600, hero.Co2Ppm);
        Assert.Equal(posted, dispatcher.Posted);
    }

    [Fact]
    public async Task A_failing_chart_refresh_is_logged_not_thrown()
    {
        await SelectDeviceAsync();
        AddHour(600);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        flaky.Fail = true;

        tabs.Select(Metric.Humidity, LiveRange.SixHours);

        await Wait.Until(() => logger.Entries.Any(entry => entry.Level == LogLevel.Warning));
        Assert.Equal(600, hero.Co2Ppm);
    }

    [Fact]
    public async Task The_children_are_exposed_for_the_views()
    {
        Assert.Same(hero, viewModel.Hero);
        Assert.Same(gauge, viewModel.Gauge);
        Assert.Same(tabs, viewModel.Tabs);
        Assert.Same(chartCard, viewModel.ChartCard);
        Assert.Same(miniCards, viewModel.MiniCards);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Choosing_another_room_reloads_the_screen()
    {
        await SelectDeviceAsync();
        AddHour(600);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        store.Readings.Add(Samples.Co2(now.AddMinutes(-1), 1500));

        messenger.Send(new DeviceSelected(device));

        await Wait.Until(() => hero.Co2Ppm == 1500);
        Assert.Equal(1500, gauge.Value);
    }

    [Fact]
    public async Task Changing_a_setting_reloads_the_screen_so_units_follow()
    {
        await SelectDeviceAsync();
        AddHour(600);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        store.Readings.Add(Samples.Co2(now.AddMinutes(-1), 1500));

        messenger.Send(new SettingsChanged(AppSettings.Default));

        await Wait.Until(() => hero.Co2Ppm == 1500);
    }

    [Fact]
    public async Task A_finished_sync_with_new_readings_reloads_the_screen()
    {
        await SelectDeviceAsync();
        AddHour(600);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        store.Readings.Add(Samples.Co2(now.AddMinutes(-1), 1500));

        messenger.Send(new SyncCompleted(device, 30));

        await Wait.Until(() => hero.Co2Ppm == 1500);
    }

    [Fact]
    public async Task A_sync_that_added_nothing_does_not_reload()
    {
        await SelectDeviceAsync();
        AddHour(600);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        var posted = dispatcher.Posted;

        messenger.Send(new SyncCompleted(device, 0));

        Assert.Equal(posted, dispatcher.Posted);
    }

    private sealed class FlakyStore : IReadingStore
    {
        private readonly IReadingStore inner;

        public FlakyStore(IReadingStore inner)
        {
            this.inner = inner;
        }

        public bool Fail { get; set; }

        public Task<IReadOnlyList<Reading>> ReadRangeAsync(DeviceId device, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
            => Fail ? throw new IOException("disk") : inner.ReadRangeAsync(device, start, end, cancellationToken);

        public Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
            => inner.GetLastReadingTimeAsync(device, cancellationToken);

        public Task<int> MergeAsync(DeviceId device, IReadOnlyList<Reading> readings, ReadingSource source, TimeSpan interval, CancellationToken cancellationToken)
            => inner.MergeAsync(device, readings, source, interval, cancellationToken);
    }
}
