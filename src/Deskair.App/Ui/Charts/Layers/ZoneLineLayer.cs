using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class ZoneLineLayer : IChartLayer
{
    public IReadOnlyList<ChartPoint>? Points { get; set; }

    // TODO(CHART-13) [M, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Charts" (zone-coloured line); docs/design/design-tokens.md "Zone colours" (chart lines)
    // Input : Points and the chart area.
    // Output: A line whose colour follows the zone: a LinearGradientBrush mapped to y values using Zone.*.Line.
    // Rules : A null value breaks the line (a gap), never interpolates.
    // Edge  : A single point draws a dot; no points draws nothing.
    // Done  : The gallery chart sample shows the gradient line.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-13)");
}
