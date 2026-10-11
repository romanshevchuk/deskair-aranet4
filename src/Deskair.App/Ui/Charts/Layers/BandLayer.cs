using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class BandLayer : IChartLayer
{
    public double ElevatedFrom { get; set; } = 1000.0;
    public double HighAbove { get; set; } = 1400.0;

    // TODO(CHART-10) [S, haiku]
    // Spec: docs/design/design-tokens.md "Zone colours" (live chart bands); docs/design/components.md "Live chart"
    // Input : The two zone limits and the chart area.
    // Output: Three horizontal bands in Chart.Band.Good, Chart.Band.Elevated and Chart.Band.High.
    // Rules : Brushes come from the resource dictionary, not literals.
    // Edge  : Limits outside the visible range draw only the visible part.
    // Done  : A unit-free check in the gallery chart sample.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-10)");
}
