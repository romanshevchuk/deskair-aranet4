using System.Windows;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// A small smoothed line in the metric colour with an area fill and an end dot, no axes, for the metric cards. The value range is at least a minimum span per metric (<see cref="SparklineRange"/>).
/// Fewer than two values draw nothing.
/// </summary>
public sealed class Sparkline : ChartBase
{
    private const double VerticalPadding = 4;

    // Room for the end dot at the right edge (radius 2.75).
    private const double HorizontalPadding = 3;
    private const double LineThickness = 1.75;
    private const double AreaOpacity = 0.18;

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points),
        typeof(IReadOnlyList<ChartPoint>),
        typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MetricProperty = DependencyProperty.Register(
        nameof(Metric),
        typeof(Metric),
        typeof(Sparkline),
        new FrameworkPropertyMetadata(Metric.Co2, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<ChartPoint>? Points
    {
        get => (IReadOnlyList<ChartPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public Metric Metric
    {
        get => (Metric)GetValue(MetricProperty);
        set => SetValue(MetricProperty, value);
    }

    protected override Thickness PlotMargin => new(HorizontalPadding, VerticalPadding, HorizontalPadding, VerticalPadding);

    protected override ChartRanges GetDataRanges()
    {
        var points = Points?.Where(point => point.Value is not null).ToList() ?? [];

        if (points.Count == 0)
        {
            return new ChartRanges(0, 1, 0, 1);
        }

        var (low, high) = SparklineRange.Fit(Metric, points.Min(point => point.Value!.Value), points.Max(point => point.Value!.Value));

        return new ChartRanges(points.Min(point => point.Time.UtcTicks), points.Max(point => point.Time.UtcTicks), low, high);
    }

    protected override IReadOnlyList<IChartLayer> CreateLayers()
    {
        var enough = Points?.Count(point => point.Value is not null) >= 2;

        if (!enough)
        {
            return [];
        }

        var brushKey = $"Metric.{Metric}.Brush";

        return
        [
            new AreaLayer { Points = Points, BrushKey = brushKey, TopOpacity = AreaOpacity, Smooth = true },
            new LineLayer { Points = Points, BrushKey = brushKey, Thickness = LineThickness, Smooth = true },
            new EndDotLayer { Points = Points, BrushKey = brushKey }
        ];
    }
}
