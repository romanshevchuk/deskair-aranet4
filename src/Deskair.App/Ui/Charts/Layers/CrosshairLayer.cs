using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class CrosshairLayer : IChartLayer
{
    public DateTimeOffset? HoverTime { get; set; }
    public double? HoverValue { get; set; }

    // TODO(CHART-18) [S, haiku]
    // Spec: docs/design/components.md "Live chart" (crosshair, hover readout)
    // Input : Hover time and value (null = no hover) and the chart area.
    // Output: A vertical line and a dot at the hovered point.
    // Rules : Colours from Text.Strong2.
    // Edge  : Hover outside the plot draws nothing.
    // Done  : The gallery chart sample shows the crosshair on hover.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-18)");
}
