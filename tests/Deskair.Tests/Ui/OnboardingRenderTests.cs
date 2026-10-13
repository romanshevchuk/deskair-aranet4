using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.Ui.Views.Shell;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Onboarding;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Readings;
using Deskair.Sensors.Aranet4;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Sensors;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Ui;

/// <summary>
/// Renders the whole window in its first-run state to artifacts/visual, one image per step of the welcome screen, so it can be compared with the prototype.
/// Off in normal runs; switch on with the environment variable DESKAIR_RENDER=1.
/// </summary>
public class OnboardingRenderTests
{
    public enum Step
    {
        Searching,
        Found,
        NoSensor,
        BluetoothOff,
        PairFailed,
        AddingSensor
    }

    [Theory]
    [InlineData(ThemeChoice.Light, "en-US", Step.Searching)]
    [InlineData(ThemeChoice.Light, "en-US", Step.Found)]
    [InlineData(ThemeChoice.Dark, "en-US", Step.Found)]
    [InlineData(ThemeChoice.Light, "uk-UA", Step.Found)]
    [InlineData(ThemeChoice.Light, "en-US", Step.NoSensor)]
    [InlineData(ThemeChoice.Light, "en-US", Step.BluetoothOff)]
    [InlineData(ThemeChoice.Light, "en-US", Step.PairFailed)]
    [InlineData(ThemeChoice.Light, "en-US", Step.AddingSensor)]
    public void Render_the_first_run_screen(ThemeChoice theme, string culture, Step step)
    {
        RenderOutput.Require();

        var png = UiHost.Run(theme, () =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            var shell = BuildShell(step);
            var window = new MainWindow(shell);
            var content = (FrameworkElement)window.Content;

            content.DataContext = shell;
            window.Content = null;

            return ControlRenderer.ToPng(content, 1200, 700);
        });

        RenderOutput.Save($"firstrun-{theme.ToString().ToLowerInvariant()}-{culture[..2]}-{step.ToString().ToLowerInvariant()}.png", png);
    }

    private static ShellViewModel BuildShell(Step step)
    {
        var messenger = new StrongReferenceMessenger();
        var dispatcher = new FakeUiDispatcher();
        var clock = new FakeClock(new DateTimeOffset(2026, 10, 12, 18, 40, 0, TimeSpan.Zero));
        var localizer = UiHost.Localizer;
        var deviceStore = new FakeDeviceStore();
        var adding = step == Step.AddingSensor;
        if (adding)
        {
            deviceStore.Devices.Add(new DeviceInfo(new DeviceId("aranet4", "AABBCCDDEEFF"), "Office", null, [], null));
        }

        var registry = new DeviceRegistry(deviceStore);
        var settings = new SettingsService(new FakeSettingsStore(), messenger);
        var formatter = new MetricFormatter(localizer, settings);

        var footer = new FooterViewModel(registry, messenger, dispatcher, clock, localizer);
        footer.Start(adding ? ConnectionState.Normal : ConnectionState.FirstRun, null, CancellationToken.None);
        var connection = new ConnectionViewModel(messenger, dispatcher);
        connection.Update(adding ? ConnectionState.Normal : ConnectionState.FirstRun);
        registry.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        var room = new RoomChipViewModel(registry, settings, messenger, dispatcher, formatter);
        room.Load();

        var radio = new FakeBluetoothRadio { IsOn = step != Step.BluetoothOff };
        var provider = new Aranet4Provider(new FakeBleScanner(), () => new FakeAranet4Connection(Aranet4TestData.CapturedValues(), 120, 1, Aranet4TestData.CurrentReadings), new PhoneExportImporter(), clock);
        var onboarding = new OnboardingViewModel(
            [provider],
            new DeviceEnrollment(deviceStore, registry, settings, messenger),
            radio,
            localizer,
            clock,
            NullLogger<OnboardingViewModel>.Instance);
        Apply(onboarding, step);

        return new ShellViewModel { Connection = connection, Footer = footer, Room = room, Onboarding = onboarding };
    }

    private static void Apply(OnboardingViewModel onboarding, Step step)
    {
        switch (step)
        {
            case Step.Searching:
                onboarding.IsScanning = true;
                break;
            case Step.Found:
            case Step.PairFailed:
            case Step.AddingSensor:
                onboarding.Nearby =
                [
                    new DiscoveredSensor(new DeviceId("aranet4", "C3DEC8FCC7C6"), "Aranet4 0A0B3", -52),
                    new DiscoveredSensor(new DeviceId("aranet4", "AABBCCDD7C41"), "Aranet4 7C41D", -78)
                ];
                onboarding.PairFailed = step == Step.PairFailed;
                onboarding.IsOpen = step == Step.AddingSensor;
                break;
            case Step.NoSensor:
                onboarding.NoSensorFound = true;
                break;
            case Step.BluetoothOff:
                onboarding.BluetoothOff = true;
                break;
        }
    }
}
