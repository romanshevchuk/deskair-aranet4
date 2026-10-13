using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The average of the whole period (prototype): a dashed line across the plot at <see cref="Value"/> and its label ("avg 1,013") at the right end, just above the line, bold, with a halo in the
/// card colour so it reads over the bars. Nothing is drawn without a value, or when the value is outside the plot.
/// </summary>
public sealed class OverallAverageLayer : IChartLayer
{
    private const double LineOpacity = 0.55;
    private const double LabelGap = 6;
    private const double HaloWidth = 3;

    public double? Value { get; set; }

    /// <summary>The word before the number ("avg"), already localized.</summary>
    public string? Label { get; set; }

    public string Format { get; set; } = "N0";

    public CultureInfo? Culture { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Value is not { } value || value < area.YMin || value > area.YMax)
        {
            return;
        }

        var y = area.Snap(area.Y(value));
        var ink = area.Brush("Text.Primary.Brush");

        context.PushOpacity(LineOpacity);
        context.DrawLine(new Pen(ink, 1 / area.PixelsPerDip) { DashStyle = new DashStyle([4, 4], 0) }, new Point(area.Plot.Left, y), new Point(area.Plot.Right, y));
        context.Pop();

        var number = value.ToString(Format, Culture ?? CultureInfo.CurrentUICulture);
        var text = area.Text(string.IsNullOrEmpty(Label) ? number : $"{Label} {number}", ink, bold: true);
        var origin = new Point(area.Plot.Right - text.Width, y - LabelGap - text.Height);

        context.DrawGeometry(null, new Pen(area.Brush("Surface.Brush"), HaloWidth) { LineJoin = PenLineJoin.Round }, text.BuildGeometry(origin));
        context.DrawText(text, origin);
    }
}
