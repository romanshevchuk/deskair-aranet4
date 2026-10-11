using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class GapHatchLayer : IChartLayer
{
    public IReadOnlyList<ChartGap>? Gaps { get; set; }

    // TODO(CHART-17) [M, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Charts" (tiled rotated-line brush); docs/design/screens-and-states.md "History controls"
    // Input : Gaps and the chart area.
    // Output: A hatched band for each gap using a tiled, rotated-line DrawingBrush; neighbouring gaps merge.
    // Rules : The brush colour is the Text.Tertiary token at low opacity.
    // Edge  : A gap narrower than the hatch tile is drawn as a solid thin band.
    // Done  : The gallery history sample shows hatched gaps.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-17)");
}
