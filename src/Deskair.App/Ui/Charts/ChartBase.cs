using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts;

/// <summary>Shared base of all charts: scales, ordered layers, hover and hit testing.</summary>
public abstract class ChartBase : FrameworkElement
{
    /// <summary>The layers to draw, back to front. Called when the data or size changes.</summary>
    protected abstract IReadOnlyList<IChartLayer> CreateLayers();

    // TODO(CHART-01a) [L, sonnet]
    // Spec: docs/engineering/architecture.md section 8 (Charts); docs/design/components.md (all chart controls share one base).
    // Input : the DrawingContext; layers from CreateLayers().
    // Output: each layer draws in order inside the plot area with margins for axis labels.
    // Rules : build the ChartArea from the control size and the data ranges; use DynamicResource brushes via TryFindResource.
    // Edge  : zero width or height draws nothing; no data still draws the grid and bands.
    // Done  : the gallery shows an empty chart frame in light and dark (add the gallery entry with the first chart card).
    protected override void OnRender(DrawingContext drawingContext)
        => throw new NotImplementedException("TODO(CHART-01a)");

    // TODO(CHART-01b) [M, sonnet]
    // Spec: docs/design/components.md (axes, bands); docs/design/design-tokens.md "Typography" (chart text 12 px).
    // Input : the control size, data min and max for x and y.
    // Output: a ChartArea, plus helper methods to map a data value to x or y pixels and back.
    // Rules : linear scales; y grows upward; the plot area leaves room for labels at the left and bottom.
    // Edge  : min equals max widens the range so the line is not flat on the border.
    // Done  : add Tests/Ui/ChartAreaTests for round trips between data and pixels and make them pass.
    protected ChartArea CreateArea(Size size, double xMin, double xMax, double yMin, double yMax)
        => throw new NotImplementedException("TODO(CHART-01b)");

    // TODO(CHART-01c) [L, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Charts"; docs/design/components.md (hover readout, hit testing).
    // Input : the mouse position over the chart.
    // Output: the nearest data point or bucket is reported to a hover readout and the crosshair layer.
    // Rules : hit testing lives here, drawing in the layers; moving out of the chart clears the hover.
    // Edge  : gaps and no-data buckets report "no data" rather than the nearest value.
    // Done  : the gallery shows hover on the chart sample; add a pure hit-test helper test if one is extracted.
    protected override void OnMouseMove(MouseEventArgs e)
        => throw new NotImplementedException("TODO(CHART-01c)");
}
