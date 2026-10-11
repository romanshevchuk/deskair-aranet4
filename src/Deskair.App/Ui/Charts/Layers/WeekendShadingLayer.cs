using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class WeekendShadingLayer : IChartLayer
{
    public IReadOnlyList<ChartGap>? WeekendSpans { get; set; }

    // TODO(CHART-16) [S, haiku]
    // Spec: docs/design/components.md "History chart" (weekend shading)
    // Input : Time spans that are weekends (computed by the view model) and the chart area.
    // Output: A light shaded rectangle behind each span.
    // Rules : Uses the Segment.Bg brush at low opacity.
    // Edge  : Spans outside the visible range are skipped.
    // Done  : The gallery history sample shows the shading.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-16)");
}
