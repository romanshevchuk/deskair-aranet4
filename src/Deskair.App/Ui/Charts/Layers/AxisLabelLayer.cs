using System.Globalization;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

public sealed class AxisLabelLayer : IChartLayer
{
    public CultureInfo? Culture { get; set; }

    // TODO(CHART-12) [M, haiku]
    // Spec: docs/design/design-tokens.md "Typography" (chart text 12 px); docs/design/localization.md rule 4
    // Input : Axis ranges and the culture.
    // Output: Value labels on the y axis and time labels on the x axis in Text.Tertiary.
    // Rules : Numbers and dates follow the culture (1,068 versus 1 068).
    // Edge  : Labels that would overlap are skipped.
    // Done  : The gallery chart sample shows labels in English and Ukrainian.
    public void Draw(DrawingContext context, ChartArea area)
        => throw new NotImplementedException("TODO(CHART-12)");
}
