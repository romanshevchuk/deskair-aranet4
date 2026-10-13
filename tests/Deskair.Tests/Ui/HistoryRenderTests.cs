using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.Ui.Views.Shell;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.History;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Sensors.Simulated;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Queries;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Ui;

/// <summary>
/// Renders the main window on the History section with simulated readings (a gap of two days, a day recorded for only a few hours) to artifacts/visual.
/// Off in normal runs; switch on with the environment variable DESKAIR_RENDER=1.
/// </summary>
public class HistoryRenderTests
{
    private static readonly DateTimeOffset now = new(2026, 10, 12, 18, 40, 0, TimeSpan.Zero);
    private static readonly DeviceId device = new("simulated", "SIM-0001");

    [Theory]
    [InlineData(ThemeChoice.Light, "en-US", Period.Week, Metric.Co2, TemperatureUnit.Celsius, HoursFilterMode.AllDay)]
    [InlineData(ThemeChoice.Dark, "en-US", Period.Month, Metric.Co2, TemperatureUnit.Celsius, HoursFilterMode.AllDay)]
    [InlineData(ThemeChoice.Light, "uk-UA", Period.Year, Metric.Co2, TemperatureUnit.Celsius, HoursFilterMode.AllDay)]
    [InlineData(ThemeChoice.Light, "en-US", Period.Day, Metric.Co2, TemperatureUnit.Celsius, HoursFilterMode.AllDay)]
    [InlineData(ThemeChoice.Light, "en-US", Period.Week, Metric.Temperature, TemperatureUnit.Fahrenheit, HoursFilterMode.AllDay)]
    [InlineData(ThemeChoice.Light, "en-US", Period.Day, Metric.Humidity, TemperatureUnit.Celsius, HoursFilterMode.AllDay)]
    [InlineData(ThemeChoice.Light, "en-US", Period.Day, Metric.Co2, TemperatureUnit.Celsius, HoursFilterMode.Night)]
    [InlineData(ThemeChoice.Dark, "uk-UA", Period.Day, Metric.Co2, TemperatureUnit.Celsius, HoursFilterMode.Night)]
    [InlineData(ThemeChoice.Light, "en-US", Period.Day, Metric.Temperature, TemperatureUnit.Fahrenheit, HoursFilterMode.Night)]
    public async Task Render_the_history_screen(ThemeChoice theme, string culture, Period range, Metric metric, TemperatureUnit unit, HoursFilterMode hours)
    {
        RenderOutput.Require();

        UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
        var shell = await BuildShellAsync(range, metric, unit, hours);

        Write(theme, culture, shell, $"{range.ToString().ToLowerInvariant()}-{metric.ToString().ToLowerInvariant()}{(hours == HoursFilterMode.Night ? "-night" : string.Empty)}");
    }

    [Theory]
    [InlineData(ThemeChoice.Light, "en-US")]
    [InlineData(ThemeChoice.Dark, "uk-UA")]
    public async Task Render_the_history_screen_before_any_data_is_stored(ThemeChoice theme, string culture)
    {
        RenderOutput.Require();

        UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
        var shell = await BuildShellAsync(Period.Week, Metric.Co2, TemperatureUnit.Celsius, withReadings: false);

        Write(theme, culture, shell, "empty");
    }

    private static void Write(ThemeChoice theme, string culture, ShellViewModel shell, string name)
    {
        var png = UiHost.Run(theme, () =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            var window = new MainWindow(shell);
            var content = (FrameworkElement)window.Content;

            content.DataContext = shell;
            window.Content = null;

            return ControlRenderer.ToPng(content, 1200, 700);
        });

        RenderOutput.Save($"history-{theme.ToString().ToLowerInvariant()}-{culture[..2]}-{name}.png", png);
    }

    private static async Task<ShellViewModel> BuildShellAsync(Period range, Metric metric, TemperatureUnit unit, HoursFilterMode hours = HoursFilterMode.AllDay, bool withReadings = true)
    {
        var messenger = new StrongReferenceMessenger();
        var dispatcher = new FakeUiDispatcher();
        var clock = new FakeClock(now);
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);
        var localizer = UiHost.Localizer;

        var store = new FakeReadingStore();

        for (var time = now.AddDays(-400); time <= now; time += TimeSpan.FromMinutes(2))
        {
            var day = (now - time).TotalDays;
            var inGap = day is > 3 and < 5;
            var partialDay = time.Date == new DateTime(2026, 10, 9) && time.Hour is < 6 or > 9 && time.Date == new DateTime(2026, 10, 9);

            if (withReadings && !inGap && !partialDay)
            {
                store.Readings.Add(SimulatedSensor.ReadingAt(time));
            }
        }

        var deviceStore = new FakeDeviceStore();
        deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [new IntervalChange(now.AddYears(-2), TimeSpan.FromMinutes(2))], null));
        var registry = new DeviceRegistry(deviceStore);
        await registry.LoadAsync(CancellationToken.None);

        var settingsStore = new FakeSettingsStore { Stored = AppSettings.Default with { TemperatureUnit = unit } };
        var settings = new SettingsService(settingsStore, messenger);
        await settings.LoadAsync(CancellationToken.None);
        var formatter = new MetricFormatter(localizer, settings);

        var toolbar = new HistoryToolbarViewModel { Range = range, Metric = metric, Hours = hours };
        var stats = new StatsViewModel(formatter, localizer, calendar);
        var history = new HistoryViewModel(
            toolbar,
            stats,
            new TypicalDayViewModel(),
            new SleepViewModel(localizer, formatter, calendar),
            TestExport.Create(store, registry),
            new ReadingQueryService(store, registry, clock, calendar),
            registry,
            calendar,
            clock,
            messenger,
            formatter,
            NullLogger<HistoryViewModel>.Instance);
        await history.RefreshAsync(CancellationToken.None);

        var footer = new FooterViewModel(registry, messenger, dispatcher, clock, localizer);
        footer.Start(ConnectionState.Normal, now.AddSeconds(-20), CancellationToken.None);
        var connection = new ConnectionViewModel(messenger, dispatcher, localizer);
        connection.Update(ConnectionState.Normal);
        var room = new RoomChipViewModel(registry, settings, messenger, dispatcher, formatter);
        room.Load();

        return new ShellViewModel { Connection = connection, Footer = footer, Room = room, History = history, Section = ShellSection.History };
    }
}
