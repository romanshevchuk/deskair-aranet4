using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class RangeBarLayer : IChartLayer
{
    public IReadOnlyList<ChartBar>? Bars { get; set; }

    // TODO(CHART-14) [M, haiku]
    // Spec: docs/design/screens-and-states.md "History controls" (bucket state table)
    // Input : Bars and the chart area.
    // Output: Floating min to max bars. Complete = normal; Partial = lighter with a dashed outline; NoData = nothing (the gap layer hatches it).
    // Rules : Bars use the metric colour.
    // Edge  : A bar with equal min and max still shows at least 2 px.
    // Done  : The gallery history sample shows the three states.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-14)");
}
