using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
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

    private static string OutputFolder([CallerFilePath] string thisFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "artifacts", "visual"));

    [Theory]
    [InlineData(ThemeChoice.Light, "en-US", false)]
    [InlineData(ThemeChoice.Dark, "en-US", false)]
    [InlineData(ThemeChoice.Light, "uk-UA", false)]
    [InlineData(ThemeChoice.Light, "en-US", true)]
    [InlineData(ThemeChoice.Dark, "uk-UA", true)]
    public void Render_the_flyout(ThemeChoice theme, string culture, bool notConnected)
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("DESKAIR_RENDER") == "1", "Set DESKAIR_RENDER=1 to write the images.");

        var png = UiHost.Run(theme, () =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            var localizer = UiHost.Localizer;
            var viewModel = new FlyoutViewModel(localizer, TestFormatter.Create(localizer), new LocalCalendar(TimeZoneInfo.Utc), new FakeWindowController());
            var recent = new List<Reading>();

            for (var time = now.AddHours(-24); time <= now; time += TimeSpan.FromMinutes(2))
            {
                recent.Add(SimulatedSensor.ReadingAt(time));
            }

            viewModel.Update(notConnected ? null : recent[^1], notConnected ? [] : recent, now, "Office");

            var view = new FlyoutView { DataContext = viewModel };

            return ControlRenderer.ToPng(view, 420);
        });

        Directory.CreateDirectory(OutputFolder());
        File.WriteAllBytes(Path.Combine(OutputFolder(), $"flyout-{theme.ToString().ToLowerInvariant()}-{culture[..2]}-{(notConnected ? "offline" : "live")}.png"), png);
    }
}
