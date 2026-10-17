using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.Ui.Views.Flyout;
using Deskair.App.ViewModels.Flyout;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Sensors.Simulated;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Ui;

/// <summary>Renders the flyout content to artifacts/visual. Off in normal runs; switch on with the environment variable DESKAIR_RENDER=1.</summary>
public class FlyoutRenderTests
{
    private static readonly DateTimeOffset now = new(2026, 10, 12, 18, 40, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ThemeChoice.Light, "en-US", false)]
    [InlineData(ThemeChoice.Dark, "en-US", false)]
    [InlineData(ThemeChoice.Light, "uk-UA", false)]
    [InlineData(ThemeChoice.Light, "en-US", true)]
    [InlineData(ThemeChoice.Dark, "uk-UA", true)]
    [InlineData(ThemeChoice.Light, "en-US", false, true)]
    [InlineData(ThemeChoice.Dark, "en-US", true)]
    [InlineData(ThemeChoice.Light, "en-US", true, false, 0, true)]
    [InlineData(ThemeChoice.Light, "en-US", false, false, 600)]
    [InlineData(ThemeChoice.Dark, "en-US", false, false, -450)]
    public void Render_the_flyout(ThemeChoice theme, string culture, bool notConnected, bool snoozed = false, double co2Shift = 0, bool bluetoothOff = false)
    {
        RenderOutput.Require();

        var png = UiHost.Run(theme, () =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            var localizer = UiHost.Localizer;
            var calendar = new LocalCalendar(TimeZoneInfo.Utc);
            var clock = new FakeClock(now);
            var (engine, snooze) = TestSnooze.Create(clock, new StrongReferenceMessenger());
            using var alerts = engine;

            if (snoozed)
            {
                snooze.ForOneHour();
            }

            var viewModel = new FlyoutViewModel(localizer, TestFormatter.Create(localizer), calendar, new FakeWindowController(), snooze, new FriendlyTime(localizer, calendar, clock), new FakeBluetoothRadio { IsOn = !bluetoothOff });
            var recent = new List<Reading>();

            for (var time = now.AddHours(-24); time <= now; time += TimeSpan.FromMinutes(2))
            {
                var reading = SimulatedSensor.ReadingAt(time);
                recent.Add(reading with { Co2Ppm = reading.Co2Ppm + co2Shift });
            }

            // Connected: the reading is 2 minutes old. Not connected: it was last seen 25 minutes ago.
            viewModel.Update(notConnected ? null : recent[^1], recent, now.AddMinutes(notConnected ? 25 : 2), "Office");

            var view = new FlyoutView { DataContext = viewModel };

            return ControlRenderer.ToPng(view, 420);
        });

        RenderOutput.Save($"flyout-{theme.ToString().ToLowerInvariant()}-{culture[..2]}-{(notConnected ? (bluetoothOff ? "offline-bluetoothoff" : "offline") : snoozed ? "snoozed" : co2Shift < 0 ? "good" : co2Shift > 0 ? "high" : "live")}.png", png);
    }
}
