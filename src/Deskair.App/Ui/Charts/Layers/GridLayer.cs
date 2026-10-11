using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class GridLayer : IChartLayer
{
    public int YTickCount { get; set; } = 4;

    // TODO(CHART-11) [S, haiku]
    // Spec: docs/design/design-tokens.md "Neutrals" (grid)
    // Input : Tick count and the chart area.
    // Output: Horizontal gridlines in the Grid brush at evenly spaced values.
    // Rules : 1 px lines, pixel aligned.
    // Edge  : Tick count of 0 draws nothing.
    // Done  : The gallery chart sample shows the grid.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-11)");
}
