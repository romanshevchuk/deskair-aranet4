using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Sensors;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public sealed class FooterViewModelTests : IDisposable
{
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 12, 12);
    private static readonly DeviceId device = new("aranet4", "0A0B3");

    private readonly FakeClock clock = new(start);
    private readonly FakeUiDispatcher dispatcher = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly DeviceRegistry registry;
    private readonly FooterViewModel viewModel;

    public FooterViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        viewModel = new FooterViewModel(registry, messenger, dispatcher, clock, new FakeLocalizer());
    }

    public void Dispose()
    {
        messenger.UnregisterAll(viewModel);
    }

    private async Task StartAsync(ConnectionState state = ConnectionState.Normal, bool withDevice = true)
    {
        if (withDevice)
        {
            deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [], null));
            await registry.LoadAsync(TestContext.Current.CancellationToken);
        }

        viewModel.Start(state, withDevice ? start : null, CancellationToken.None);
    }

    [Fact]
    public async Task While_the_history_downloads_the_footer_says_so_with_the_progress()
    {
        await StartAsync();
        Assert.Null(viewModel.SyncText);

        messenger.Send(new SyncStarted(device));
        Assert.Equal("Footer_SyncingStart", viewModel.SyncText);

        messenger.Send(new SyncProgressed(device, new HistoryProgress(42, 154)));
        Assert.Equal("Footer_Syncing(percent=27%)", viewModel.SyncText);

        messenger.Send(new SyncProgressed(device, new HistoryProgress(154, 154)));
        Assert.Equal("Footer_Syncing(percent=100%)", viewModel.SyncText);

        // More pages than expected never show more than 100 %.
        messenger.Send(new SyncProgressed(device, new HistoryProgress(160, 154)));
        Assert.Equal("Footer_Syncing(percent=100%)", viewModel.SyncText);

        messenger.Send(new SyncCompleted(device, 5040));
        Assert.Null(viewModel.SyncText);
    }

    [Fact]
    public async Task The_hardware_id_is_in_the_tooltip_and_the_room_name_is_not_repeated()
    {
        await StartAsync();

        Assert.Equal("0A0B3", viewModel.DetailTooltip);
        Assert.DoesNotContain("0A0B3", viewModel.DetailText, StringComparison.Ordinal);
        Assert.DoesNotContain("Office", viewModel.DetailText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Without_a_sensor_it_says_so_with_a_neutral_dot()
    {
        await StartAsync(ConnectionState.FirstRun, withDevice: false);

        Assert.Equal("Footer_NoSensor", viewModel.StatusText);
        Assert.Null(viewModel.DotZone);
        Assert.Null(viewModel.DetailText);
        Assert.Null(viewModel.DetailTooltip);
        Assert.Null(viewModel.BatteryPercent);
    }

    [Fact]
    public async Task Connected_shows_a_green_dot_and_how_long_ago_the_reading_was()
    {
        await StartAsync();
        clock.Advance(TimeSpan.FromSeconds(12));
        viewModel.Refresh();

        Assert.Equal(Zone.Good, viewModel.DotZone);
        Assert.Equal("Footer_Connected(sec=12)", viewModel.StatusText);
    }

    [Fact]
    public async Task After_a_minute_the_age_is_in_minutes()
    {
        await StartAsync();
        clock.Advance(TimeSpan.FromSeconds(185));
        viewModel.Refresh();

        Assert.Equal("Footer_ConnectedMinutes(min=3)", viewModel.StatusText);
    }

    [Fact]
    public async Task Stale_data_gets_an_amber_dot_and_the_last_reading_age()
    {
        await StartAsync();
        clock.Advance(TimeSpan.FromMinutes(7));
        messenger.Send(new ConnectionStateChanged(ConnectionState.Stale));

        Assert.Equal(Zone.Elevated, viewModel.DotZone);
        Assert.Equal("Banner_Stale(min=7)", viewModel.StatusText);
    }

    [Fact]
    public async Task Not_connected_has_a_neutral_dot()
    {
        await StartAsync();
        messenger.Send(new ConnectionStateChanged(ConnectionState.NotConnected));

        Assert.Null(viewModel.DotZone);
        Assert.Equal("Footer_NotConnected", viewModel.StatusText);
    }

    [Fact]
    public async Task Connecting_has_a_neutral_dot_and_the_age_of_the_last_reading()
    {
        await StartAsync();
        clock.Advance(TimeSpan.FromMinutes(7));
        messenger.Send(new ConnectionStateChanged(ConnectionState.Connecting));

        Assert.Null(viewModel.DotZone);
        Assert.Equal("Footer_ConnectingLast(ago=Duration_Minutes(min=7))", viewModel.StatusText);
    }

    [Fact]
    public async Task Connecting_without_an_old_reading_has_only_its_line()
    {
        await StartAsync();
        messenger.Send(new ConnectionStateChanged(ConnectionState.Connecting));

        Assert.Equal("Footer_Connecting", viewModel.StatusText);
    }

    [Fact]
    public async Task A_low_battery_keeps_the_green_dot()
    {
        await StartAsync();
        messenger.Send(new ConnectionStateChanged(ConnectionState.LowBattery));

        Assert.Equal(Zone.Good, viewModel.DotZone);
    }

    [Fact]
    public async Task Sensor_model_and_battery_are_shown_once_the_sensor_has_reported()
    {
        await StartAsync();

        messenger.Send(new SensorStatusUpdated(device, new SensorStatus(TimeSpan.FromMinutes(2), 65, -70)));

        Assert.Equal("Sensor_Aranet4_Name", viewModel.DetailText);
        Assert.Equal("0A0B3", viewModel.DetailTooltip);
        Assert.Equal(65, viewModel.BatteryPercent);
        Assert.Equal("65%", viewModel.BatteryText);
    }

    [Fact]
    public async Task Without_a_battery_level_only_the_sensor_model_is_shown()
    {
        await StartAsync();

        messenger.Send(new SensorStatusUpdated(device, new SensorStatus(TimeSpan.FromMinutes(2), null, null)));

        Assert.Equal("Sensor_Aranet4_Name", viewModel.DetailText);
        Assert.Null(viewModel.BatteryPercent);
        Assert.Null(viewModel.BatteryText);
    }

    [Fact]
    public async Task A_new_reading_restarts_the_age()
    {
        await StartAsync();
        clock.Advance(TimeSpan.FromSeconds(50));

        messenger.Send(new ReadingReceived(device, Samples.Co2(clock.GetUtcNow(), 700)));
        clock.Advance(TimeSpan.FromSeconds(5));
        viewModel.Refresh();

        Assert.Equal("Footer_Connected(sec=5)", viewModel.StatusText);
    }

    [Fact]
    public async Task Choosing_another_room_clears_what_was_known_about_the_old_one()
    {
        await StartAsync();
        messenger.Send(new SensorStatusUpdated(device, new SensorStatus(TimeSpan.FromMinutes(2), 65, -70)));

        messenger.Send(new DeviceSelected(device));

        Assert.Null(viewModel.BatteryPercent);
        Assert.Equal("Sensor_Aranet4_Name", viewModel.DetailText);
    }

    [Fact]
    public async Task The_background_tick_keeps_the_age_current()
    {
        await StartAsync();
        using var cts = new CancellationTokenSource();
        viewModel.Start(ConnectionState.Normal, start, cts.Token);

        clock.Advance(TimeSpan.FromSeconds(1));
        clock.Advance(TimeSpan.FromSeconds(1));

        await Wait.Until(() => viewModel.StatusText == "Footer_Connected(sec=2)");
        await cts.CancelAsync();
    }

    [Fact]
    public async Task The_low_battery_text_is_only_there_when_the_battery_is_low()
    {
        await StartAsync();
        messenger.Send(new SensorStatusUpdated(device, new SensorStatus(TimeSpan.FromMinutes(2), 8, -60)));
        Assert.Null(viewModel.LowBatteryText);

        messenger.Send(new ConnectionStateChanged(ConnectionState.LowBattery));

        Assert.Equal("Banner_LowBattery(percent=8%)", viewModel.LowBatteryText);
    }

    private async Task TickAsync()
    {
        await Wait.Until(() => clock.PendingTimers > 0);
        clock.Advance(TimeSpan.FromSeconds(1));
        await Wait.Until(() => clock.PendingTimers > 0);
    }

    [Fact]
    public async Task A_hidden_window_gets_no_footer_ticks()
    {
        await StartAsync();
        viewModel.SetWindowShown(false);
        var postedBefore = dispatcher.Posted;

        for (var tick = 0; tick < 10; tick++)
        {
            await TickAsync();
        }

        Assert.Equal(postedBefore, dispatcher.Posted);
    }

    [Fact]
    public async Task Showing_the_window_refreshes_the_footer_at_once()
    {
        await StartAsync();
        viewModel.SetWindowShown(false);
        await Wait.Until(() => clock.PendingTimers > 0);
        clock.Advance(TimeSpan.FromMinutes(3));
        await Wait.Until(() => clock.PendingTimers > 0);
        Assert.Equal("Footer_Connected(sec=0)", viewModel.StatusText);
        var postedBefore = dispatcher.Posted;

        viewModel.SetWindowShown(true);

        Assert.Equal(postedBefore + 1, dispatcher.Posted);
        Assert.Equal("Footer_ConnectedMinutes(min=3)", viewModel.StatusText);
    }

    [Fact]
    public async Task Ticks_resume_after_the_window_is_shown_again()
    {
        await StartAsync();
        viewModel.SetWindowShown(false);
        viewModel.SetWindowShown(true);
        var postedBefore = dispatcher.Posted;

        for (var tick = 0; tick < 3; tick++)
        {
            await TickAsync();
        }

        Assert.Equal(postedBefore + 3, dispatcher.Posted);
    }
}
