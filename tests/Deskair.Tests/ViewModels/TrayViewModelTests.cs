using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Tray;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class TrayViewModelTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);
    private static readonly DeviceId device = new("test", "A1");
    private static readonly DeviceId other = new("test", "B2");

    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly DeviceRegistry registry;
    private readonly FakeSettingsStore settingsStore = new();
    private readonly SettingsService settings;
    private readonly FakeDoNotDisturb doNotDisturb = new();

    public TrayViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        settings = new SettingsService(settingsStore, messenger);
    }

    private async Task<TrayViewModel> CreateAsync(bool withDevice = true, ILocalizer? localizer = null)
    {
        if (withDevice)
        {
            deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [], null));
        }

        await registry.LoadAsync(TestContext.Current.CancellationToken);
        localizer ??= new FakeLocalizer();

        return new TrayViewModel(registry, localizer, TestFormatter.Create(localizer), messenger, new FakeUiDispatcher(), settings, doNotDisturb);
    }

    private void Reading(double ppm, DeviceId? from = null)
        => messenger.Send(new ReadingReceived(from ?? device, Samples.Co2(now, ppm)));

    [Fact]
    public async Task Without_a_sensor_the_icon_is_grey_and_the_tooltip_is_the_app_name()
    {
        var viewModel = await CreateAsync(withDevice: false);

        Assert.Null(viewModel.Zone);
        Assert.Null(viewModel.Co2);
        Assert.Equal("Tray_AppName", viewModel.Tooltip);
    }

    [Fact]
    public async Task A_reading_gives_the_zone_the_value_and_the_tooltip()
    {
        var viewModel = await CreateAsync();

        Reading(1068);

        Assert.Equal(Zone.Elevated, viewModel.Zone);
        Assert.Equal(1068, viewModel.Co2);
        Assert.Equal("Office · 1068 ppm · Zone_Elevated", viewModel.Tooltip);
        Assert.Equal(viewModel.Tooltip, viewModel.StatusText);
    }

    [Theory]
    [InlineData(500, Zone.Good)]
    [InlineData(1000, Zone.Elevated)]
    [InlineData(1500, Zone.High)]
    public async Task The_zone_follows_the_thresholds(double ppm, Zone expected)
    {
        var viewModel = await CreateAsync();

        Reading(ppm);

        Assert.Equal(expected, viewModel.Zone);
    }

    [Fact]
    public async Task A_reading_of_another_room_is_ignored()
    {
        var viewModel = await CreateAsync();

        Reading(1500, other);

        Assert.Null(viewModel.Zone);
        Assert.Equal("Office", viewModel.Tooltip);
    }

    [Fact]
    public async Task A_room_with_no_reading_yet_shows_only_its_name()
    {
        var viewModel = await CreateAsync();

        Assert.Equal("Office", viewModel.Tooltip);
        Assert.Null(viewModel.Zone);
    }

    [Fact]
    public async Task Not_connected_turns_the_icon_grey_and_says_so()
    {
        var viewModel = await CreateAsync();
        Reading(1068);

        messenger.Send(new ConnectionStateChanged(ConnectionState.NotConnected));

        Assert.Null(viewModel.Zone);
        Assert.Null(viewModel.Co2);
        Assert.Equal("Office · Footer_NotConnected", viewModel.Tooltip);
    }

    [Fact]
    public async Task The_icon_colour_returns_with_the_next_reading()
    {
        var viewModel = await CreateAsync();
        messenger.Send(new ConnectionStateChanged(ConnectionState.NotConnected));

        messenger.Send(new ConnectionStateChanged(ConnectionState.Normal));
        Reading(700);

        Assert.Equal(Zone.Good, viewModel.Zone);
    }

    [Theory]
    [InlineData(ConnectionState.Stale)]
    [InlineData(ConnectionState.LowBattery)]
    public async Task A_stale_sensor_or_a_low_battery_keeps_the_last_zone(ConnectionState state)
    {
        var viewModel = await CreateAsync();
        Reading(1068);

        messenger.Send(new ConnectionStateChanged(state));

        Assert.Equal(Zone.Elevated, viewModel.Zone);
        Assert.Equal("Office · 1068 ppm · Zone_Elevated", viewModel.Tooltip);
    }

    [Fact]
    public async Task First_run_is_grey_with_the_app_name()
    {
        var viewModel = await CreateAsync();
        Reading(1068);

        messenger.Send(new ConnectionStateChanged(ConnectionState.FirstRun));

        Assert.Null(viewModel.Zone);
        Assert.Equal("Tray_AppName", viewModel.Tooltip);
    }

    [Fact]
    public async Task Choosing_another_room_forgets_the_reading()
    {
        var viewModel = await CreateAsync();
        Reading(1068);

        messenger.Send(new DeviceSelected(other));

        Assert.Null(viewModel.Zone);
        Assert.Null(viewModel.Co2);
    }

    [Fact]
    public async Task Renaming_the_room_changes_the_tooltip()
    {
        var viewModel = await CreateAsync();
        Reading(1068);
        deviceStore.Devices[0] = deviceStore.Devices[0] with { RoomName = "Study" };
        await registry.LoadAsync(TestContext.Current.CancellationToken);

        messenger.Send(new DeviceRenamed(device));

        Assert.Equal("Study · 1068 ppm · Zone_Elevated", viewModel.Tooltip);
    }

    [Fact]
    public async Task A_new_language_rebuilds_the_tooltip()
    {
        var localizer = new SwitchableLocalizer();
        var viewModel = await CreateAsync(localizer: localizer);
        Reading(1068);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        localizer.Prefix = "uk:";

        messenger.Send(new LanguageChanged("uk"));

        Assert.Contains(nameof(TrayViewModel.Tooltip), changed);
        Assert.Contains(nameof(TrayViewModel.StatusText), changed);
    }

    [Fact]
    public async Task The_icon_shows_the_ring_unless_the_value_is_asked_for()
    {
        var viewModel = await CreateAsync();

        Assert.False(viewModel.ShowValue);
    }

    [Fact]
    public async Task The_value_on_the_icon_follows_the_setting_at_start_and_when_it_changes()
    {
        settingsStore.Stored = AppSettings.Default with { ShowValueOnTrayIcon = true };
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        var viewModel = await CreateAsync();
        Assert.True(viewModel.ShowValue);

        await settings.UpdateAsync(current => current with { ShowValueOnTrayIcon = false }, TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShowValue);
    }

    [Fact]
    public async Task While_windows_holds_alerts_the_tooltip_says_so()
    {
        var viewModel = await CreateAsync();
        Reading(1068);

        messenger.Send(new DoNotDisturbChanged(true));

        Assert.Equal("Office · 1068 ppm · Zone_Elevated · Tray_AlertsPausedByWindows", viewModel.Tooltip);
    }

    [Fact]
    public async Task The_tooltip_is_back_to_normal_when_do_not_disturb_ends()
    {
        var viewModel = await CreateAsync();
        Reading(1068);
        messenger.Send(new DoNotDisturbChanged(true));

        messenger.Send(new DoNotDisturbChanged(false));

        Assert.Equal("Office · 1068 ppm · Zone_Elevated", viewModel.Tooltip);
    }

    [Fact]
    public async Task Do_not_disturb_already_on_at_start_is_shown()
    {
        doNotDisturb.IsActive = true;

        var viewModel = await CreateAsync();

        Assert.Equal("Office · Tray_AlertsPausedByWindows", viewModel.Tooltip);
    }

    [Fact]
    public async Task With_alerts_switched_off_nothing_is_paused_so_nothing_is_said()
    {
        doNotDisturb.IsActive = true;
        settingsStore.Stored = AppSettings.Default with { AlertsEnabled = false };
        await settings.LoadAsync(TestContext.Current.CancellationToken);

        var viewModel = await CreateAsync();

        Assert.Equal("Office", viewModel.Tooltip);
    }
}
