using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// The plot rectangle and the data ranges it shows. Layers use it to turn data values into pixels and to look up brushes.
/// Scales are linear and y grows upward. Time values are UTC ticks, so <see cref="X(DateTimeOffset)"/> and <see cref="X(double)"/> agree.
/// </summary>
public sealed record ChartArea(Rect Plot, double XMin, double XMax, double YMin, double YMax)
{
    /// <summary>Finds a resource (brush) by key; the chart control supplies its own resource lookup.</summary>
    public Func<string, object?> Resources { get; init; } = _ => null;

    public Typeface Typeface { get; init; } = new("Segoe UI");

    /// <summary>Chart text size in device-independent pixels (12 px minimum, design-tokens.md).</summary>
    public double FontSize { get; init; } = 12;

    public double PixelsPerDip { get; init; } = 1;

    /// <summary>
    /// Builds the plot area inside <paramref name="size"/>, leaving <paramref name="margin"/> for labels. A range whose minimum equals
    /// its maximum is widened so a flat line does not sit on the border; a size too small for the margins gives an empty plot.
    /// </summary>
    public static ChartArea Create(Size size, Thickness margin, double xMin, double xMax, double yMin, double yMax)
    {
        var width = Math.Max(0, size.Width - margin.Left - margin.Right);
        var height = Math.Max(0, size.Height - margin.Top - margin.Bottom);
        var plot = new Rect(margin.Left, margin.Top, width, height);

        (xMin, xMax) = Widen(xMin, xMax);
        (yMin, yMax) = Widen(yMin, yMax);

        return new ChartArea(plot, xMin, xMax, yMin, yMax);
    }

    public bool IsEmpty => Plot.Width <= 0 || Plot.Height <= 0;

    public double X(double value)
        => Plot.Left + (((value - XMin) / (XMax - XMin)) * Plot.Width);

    public double X(DateTimeOffset time) => X((double)time.UtcTicks);

    public double Y(double value)
        => Plot.Bottom - (((value - YMin) / (YMax - YMin)) * Plot.Height);

    public double DataX(double pixel)
        => XMin + (((pixel - Plot.Left) / Plot.Width) * (XMax - XMin));

    public DateTimeOffset TimeAt(double pixel)
        => new((long)Math.Round(DataX(pixel)), TimeSpan.Zero);

    public double DataY(double pixel)
        => YMin + (((Plot.Bottom - pixel) / Plot.Height) * (YMax - YMin));

    public bool ContainsX(double pixel) => pixel >= Plot.Left && pixel <= Plot.Right;

    /// <summary>The brush for a resource key, or transparent when the key is unknown (so drawing never throws).</summary>
    public Brush Brush(string key) => Resources(key) as Brush ?? Brushes.Transparent;

    /// <summary>Snaps a coordinate to the middle of a device pixel so a 1 px line is crisp.</summary>
    public double Snap(double coordinate)
    {
        var step = 1 / PixelsPerDip;

        return (Math.Floor(coordinate / step) * step) + (step / 2);
    }

    public FormattedText Text(string text, Brush brush, bool bold = false)
    {
        var typeface = bold ? new Typeface(Typeface.FontFamily, Typeface.Style, FontWeights.SemiBold, Typeface.Stretch) : Typeface;

        return new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, FontSize, brush, PixelsPerDip);
    }

    private static (double Min, double Max) Widen(double min, double max)
    {
        if (max > min)
        {
            return (min, max);
        }

        var padding = Math.Max(1, Math.Abs(min) * 0.05);

        return (min - padding, max + padding);
    }
}
