using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.Ui.Views.Shell;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Settings;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Readings;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Settings;
using Deskair.Storage.Export;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Ui;

/// <summary>
/// Renders the main window on the Settings section with real view models to artifacts/visual.
/// Off in normal runs; switch on with the environment variable DESKAIR_RENDER=1.
/// </summary>
public class SettingsRenderTests
{
    private static string OutputFolder([CallerFilePath] string thisFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "artifacts", "visual"));

    [Theory]
    [InlineData(ThemeChoice.Light, "en-US")]
    [InlineData(ThemeChoice.Dark, "en-US")]
    [InlineData(ThemeChoice.Light, "uk-UA")]
    public void Render_the_settings_screen(ThemeChoice theme, string culture)
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("DESKAIR_RENDER") == "1", "Set DESKAIR_RENDER=1 to write the window images.");

        var png = UiHost.Run(theme, () =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            var shell = BuildShell(theme, culture);
            var window = new MainWindow(shell);
            var content = (FrameworkElement)window.Content;

            content.DataContext = shell;
            window.Content = null;

            return ControlRenderer.ToPng(content, 1200, 700);
        });

        Directory.CreateDirectory(OutputFolder());
        File.WriteAllBytes(Path.Combine(OutputFolder(), $"settings-{theme.ToString().ToLowerInvariant()}-{culture[..2]}.png"), png);
    }

    private static ShellViewModel BuildShell(ThemeChoice theme, string culture)
    {
        var messenger = new StrongReferenceMessenger();
        var dispatcher = new FakeUiDispatcher();
        var clock = new FakeClock(new DateTimeOffset(2026, 10, 12, 18, 40, 0, TimeSpan.Zero));
        var localizer = UiHost.Localizer;
        var deviceStore = new FakeDeviceStore();
        var device = new DeviceId("aranet4", "C3DEC8FCC7C6");
        deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [], null));
        deviceStore.Devices.Add(new DeviceInfo(new DeviceId("aranet4", "AABBCCDD7C41"), "Bedroom", null, [], null));
        var registry = new DeviceRegistry(deviceStore);
        registry.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

        var settingsStore = new FakeSettingsStore
        {
            Stored = AppSettings.Default with
            {
                Theme = theme,
                Language = culture.StartsWith("uk", StringComparison.Ordinal) ? LanguageChoice.Ukrainian : LanguageChoice.English,
                QuietHoursEnabled = true
            }
        };
        var settings = new SettingsService(settingsStore, messenger);
        settings.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        var formatter = new MetricFormatter(localizer, settings);

        var footer = new FooterViewModel(registry, messenger, dispatcher, clock, localizer);
        footer.Start(ConnectionState.Normal, clock.GetUtcNow().AddSeconds(-20), CancellationToken.None);
        var connection = new ConnectionViewModel(messenger, dispatcher);
        connection.Update(ConnectionState.Normal);
        var room = new RoomChipViewModel(registry, settings, messenger, dispatcher, formatter);
        room.Load();

        var screen = new SettingsViewModel(
            new AppearanceSettingsViewModel(settings, NullLogger<AppearanceSettingsViewModel>.Instance),
            new AlertsSettingsViewModel(settings, NullLogger<AlertsSettingsViewModel>.Instance),
            new DevicesSettingsViewModel(registry, new DeviceEnrollment(deviceStore, registry, settings, messenger), messenger, NullLogger<DevicesSettingsViewModel>.Instance),
            Deskair.Tests.ViewModels.DataSettingsViewModelTests.Create(new TempKnownFolders()),
            new StartupSettingsViewModel(new FakeStartupRegistration { IsEnabled = true }, NullLogger<StartupSettingsViewModel>.Instance));
        screen.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

        return new ShellViewModel { Connection = connection, Footer = footer, Room = room, Settings = screen, Section = ShellSection.Settings };
    }
}
