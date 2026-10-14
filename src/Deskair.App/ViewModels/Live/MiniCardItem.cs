using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;

namespace Deskair.App.ViewModels.Live;

/// <summary>
/// What one metric mini card shows. Text is already localized and formatted. <see cref="IsSelected"/> is set when the Live chart shows this metric;
/// <see cref="AccessibleName"/> is what Narrator reads ("Temperature, 21.1 °C, Steady").
/// </summary>
public sealed record MiniCardItem(
    Metric Metric,
    string Title,
    string? ValueText,
    string? SubtitleText,
    IReadOnlyList<ChartPoint> Sparkline,
    string? UnitText = null,
    bool IsSelected = false,
    string? AccessibleName = null);
