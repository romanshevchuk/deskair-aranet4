using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;

namespace Deskair.App.ViewModels.Live;

/// <summary>What one metric mini card shows. Text is already localized and formatted.</summary>
public sealed record MiniCardItem(
    Metric Metric,
    string Title,
    string? ValueText,
    string? SubtitleText,
    IReadOnlyList<ChartPoint> Sparkline,
    string? UnitText = null);
