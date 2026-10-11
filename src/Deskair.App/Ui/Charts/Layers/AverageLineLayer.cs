using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class AverageLineLayer : IChartLayer
{
    public IReadOnlyList<ChartBar>? Bars { get; set; }

    // TODO(CHART-15) [S, haiku]
    // Spec: docs/design/components.md "History chart" (average tick)
    // Input : Bars and the chart area.
    // Output: A short horizontal tick at each bar average.
    // Rules : Skip bars with a null average.
    // Edge  : No bars draws nothing.
    // Done  : The gallery history sample shows the ticks.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-15)");
}
