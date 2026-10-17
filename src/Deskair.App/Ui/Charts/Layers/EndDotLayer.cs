using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>A filled dot at the latest value, in the line's colour (the sparklines: 5.5 px across).</summary>
public sealed class EndDotLayer : IChartLayer
{
    public IReadOnlyList<ChartPoint>? Points { get; set; }

    /// <summary>The resource key of the dot brush, for example "Metric.Temperature.Brush".</summary>
    public string BrushKey { get; set; } = "Text.Primary.Brush";

    public double Radius { get; set; } = 2.75;

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Points is not { Count: > 0 } points || ChartLines.LastValue(points) is not { Value: { } value } last)
        {
            return;
        }

        context.DrawEllipse(area.Brush(BrushKey), null, new Point(area.X(last.Time), area.Y(value)), Radius, Radius);
    }
}
