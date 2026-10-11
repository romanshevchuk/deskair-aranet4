using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class DashedZoneLinesLayer : IChartLayer
{
    public double ElevatedFrom { get; set; } = 1000.0;
    public double HighAbove { get; set; } = 1400.0;

    // TODO(CHART-19) [S, haiku]
    // Spec: docs/design/components.md "Live chart" (dashed zone lines)
    // Input : The two zone limits and the chart area.
    // Output: Dashed horizontal lines at both limits in the Zone.*.Line colours.
    // Rules : 1 px dashed lines.
    // Edge  : Limits outside the visible range are skipped.
    // Done  : The gallery chart sample shows the dashed lines.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-19)");
}
