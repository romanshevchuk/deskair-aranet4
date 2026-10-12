using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.Live;

/// <summary>What the hero card and the tray flyout both show about the air right now. Text is already localized.</summary>
public sealed record AirSummary(
    Zone Zone,
    double Co2Ppm,
    TrendDirection Trend,
    string TrendText,
    string? TrendDetail,
    string AdviceTitle,
    string AdviceText);
