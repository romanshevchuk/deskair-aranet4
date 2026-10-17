using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.Ui.Views.Shell;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Live;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Sensors.Simulated;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Queries;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Ui;

/// <summary>
/// Renders the main window with real view models and simulated readings to artifacts/visual, so the Live screen can be compared with the prototype.
/// Off in normal runs; switch on with the environment variable DESKAIR_RENDER=1.
/// </summary>
public class MainWindowRenderTests
{
    // The simulated room is at about 1,050 ppm and falling at this time of day.
    private static readonly DateTimeOffset now = new(2026, 10, 12, 18, 40, 0, TimeSpan.Zero);
    private static readonly DeviceId device = new("simulated", "SIM-0001");

    [Theory]
    [InlineData(ThemeChoice.Light, "en-US", ConnectionState.Normal, TemperatureUnit.Celsius)]
    [InlineData(ThemeChoice.Dark, "en-US", ConnectionState.Normal, TemperatureUnit.Celsius)]
    [InlineData(ThemeChoice.Light, "uk-UA", ConnectionState.Normal, TemperatureUnit.Celsius)]
    [InlineData(ThemeChoice.Light, "en-US", ConnectionState.Stale, TemperatureUnit.Celsius)]
    [InlineData(ThemeChoice.Light, "en-US", ConnectionState.LowBattery, TemperatureUnit.Fahrenheit)]
    [InlineData(ThemeChoice.Light, "en-US", ConnectionState.NotConnected, TemperatureUnit.Celsius)]
    [InlineData(ThemeChoice.Dark, "en-US", ConnectionState.NotConnected, TemperatureUnit.Celsius)]
    [InlineData(ThemeChoice.Dark, "en-US", ConnectionState.Stale, TemperatureUnit.Celsius)]
    [InlineData(ThemeChoice.Dark, "en-US", ConnectionState.LowBattery, TemperatureUnit.Celsius)]
    [InlineData(ThemeChoice.Dark, "uk-UA", ConnectionState.NotConnected, TemperatureUnit.Celsius)]
    public void Render_the_live_screen(ThemeChoice theme, string culture, ConnectionState state, TemperatureUnit unit)
    {
        RenderOutput.Require();

        var png = UiHost.Run(theme, () =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            // The dark Ukrainian offline image is the one with Bluetooth off.
            var shell = BuildShell(state, unit, bluetoothOff: theme == ThemeChoice.Dark && culture == "uk-UA");
            var window = new MainWindow(shell);
            var content = (FrameworkElement)window.Content;

            // The content takes its data context from the window; keep it when the content is moved out for drawing.
            content.DataContext = shell;
            window.Content = null;

            return ControlRenderer.ToPng(content, 1200, 700);
        });

        RenderOutput.Save($"live-{theme.ToString().ToLowerInvariant()}-{culture[..2]}-{state.ToString().ToLowerInvariant()}-{unit.ToString().ToLowerInvariant()}.png", png);
    }

    // The simulated room is Elevated; shifting the CO2 down gives Good air and up gives High, so each zone and theme can be looked at.
    [Theory]
    [InlineData(ThemeChoice.Light, -450)]
    [InlineData(ThemeChoice.Dark, -450)]
    [InlineData(ThemeChoice.Light, 600)]
    [InlineData(ThemeChoice.Dark, 600)]
    public void Render_the_live_screen_in_the_good_and_high_zones(ThemeChoice theme, double co2Shift)
    {
        RenderOutput.Require();

        var png = UiHost.Run(theme, () =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo("en-US"));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            var shell = BuildShell(ConnectionState.Normal, TemperatureUnit.Celsius, co2Shift: co2Shift);
            var window = new MainWindow(shell);
            var content = (FrameworkElement)window.Content;

            content.DataContext = shell;
            window.Content = null;

            return ControlRenderer.ToPng(content, 1200, 700);
        });

        RenderOutput.Save($"live-{theme.ToString().ToLowerInvariant()}-en-{(co2Shift < 0 ? "good" : "high")}.png", png);
    }

    private static ShellViewModel BuildShell(ConnectionState state, TemperatureUnit unit, bool bluetoothOff = false, double co2Shift = 0)
    {
        var messenger = new StrongReferenceMessenger();
        var dispatcher = new FakeUiDispatcher();
        var clock = new FakeClock(now);
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);
        var localizer = UiHost.Localizer;

        var store = new FakeReadingStore();
        for (var time = now.AddHours(-24); time <= now; time += TimeSpan.FromMinutes(2))
        {
            var reading = SimulatedSensor.ReadingAt(time);
            store.Readings.Add(reading with { Co2Ppm = reading.Co2Ppm + co2Shift });
        }

        var deviceStore = new FakeDeviceStore();
        deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [new IntervalChange(now.AddYears(-1), TimeSpan.FromMinutes(2))], null));
        deviceStore.Devices.Add(new DeviceInfo(new DeviceId("simulated", "SIM-0002"), "Bedroom", null, [], null));
        var registry = new DeviceRegistry(deviceStore);
        registry.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

        var settingsStore = new FakeSettingsStore { Stored = AppSettings.Default with { TemperatureUnit = unit } };
        var settings = new SettingsService(settingsStore, messenger);
        settings.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

        var formatter = new MetricFormatter(localizer, settings);
        var query = new ReadingQueryService(store, registry, clock, calendar);
        var hero = new HeroViewModel(localizer, new HeroStatsBuilder(localizer, formatter, calendar), formatter);
        var live = new LiveViewModel(
            hero,
            new GaugeViewModel(),
            new MetricTabsViewModel(),
            new ChartCardViewModel(query, registry, formatter, localizer, clock),
            new MiniCardsViewModel(localizer, formatter),
            query,
            registry,
            messenger,
            dispatcher,
            clock,
            NullLogger<LiveViewModel>.Instance);
        live.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        var footer = new FooterViewModel(registry, messenger, dispatcher, clock, localizer);
        var lastReading = state == ConnectionState.Stale ? now.AddMinutes(-7) : now.AddSeconds(-12);
        footer.Start(state, lastReading, CancellationToken.None);
        messenger.Send(new SensorStatusUpdated(device, new SensorStatus(TimeSpan.FromMinutes(2), state == ConnectionState.LowBattery ? 8 : 65, -70)));

        var room = new RoomChipViewModel(registry, settings, messenger, dispatcher, formatter);
        room.Load();
        room.Start(CancellationToken.None);
        messenger.Send(new ReadingReceived(device, store.Readings[^1]));

        var connection = new ConnectionViewModel(messenger, dispatcher, localizer);
        connection.Update(state);

        var radio = new FakeBluetoothRadio { IsOn = !bluetoothOff };
        var offline = new OfflineViewModel(registry, query, radio, messenger, dispatcher, formatter, localizer, new FriendlyTime(localizer, calendar, clock), new FakeSystemSettings());
        messenger.Send(new ReadingReceived(device, store.Readings[^1]));

        return new ShellViewModel { Live = live, Connection = connection, Footer = footer, Room = room, Offline = offline };
    }
}
