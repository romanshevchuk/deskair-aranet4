using System.Windows.Media;

namespace Deskair.App.Ui.Charts;

/// <summary>One drawing step of a chart. Layers draw in order into the chart's DrawingContext.</summary>
public interface IChartLayer
{
    void Draw(DrawingContext context, ChartArea area);
}
