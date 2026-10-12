using Deskair.App.Localization;
using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.Live;

/// <summary>Turns the latest reading and recent readings into the zone, trend and advice texts. Calculations stay in Domain; this only picks keys.</summary>
public sealed class AirSummaryBuilder
{
    private readonly ILocalizer localizer;

    public AirSummaryBuilder(ILocalizer localizer)
    {
        this.localizer = localizer;
    }

    public AirSummary Build(Reading latest, IReadOnlyList<Reading> recent, DateTimeOffset now)
    {
        var zone = ZoneClassifier.Classify(latest.Co2Ppm);
        var trend = TrendCalculator.Co2(recent, now);
        var detail = trend.Direction == TrendDirection.Steady
            ? null
            : localizer.Format("Trend_Detail", ("ppm", Math.Round(Math.Abs(trend.Change))), ("min", (int)trend.Window.TotalMinutes));
        var adviceText = zone == Zone.Good ? "Advice_Good_Text" : $"Advice_{zone}_Text_{trend.Direction}";

        return new AirSummary(
            zone,
            latest.Co2Ppm,
            trend.Direction,
            localizer.Get($"Trend_{trend.Direction}"),
            detail,
            localizer.Get($"Advice_{zone}_Title"),
            localizer.Get(adviceText));
    }
}
