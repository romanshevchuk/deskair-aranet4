using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Deskair.Domain.Zones;

namespace Deskair.App.Platform.Tray;

/// <summary>
/// Draws the notification-area icon: the logo ring in the zone colour, a grey dashed ring with a slash when the sensor is not connected, or, with a value,
/// a rounded square with the CO₂ in thousands ("1.1k"). Colours come from the zone tokens, which are the same in both themes because the taskbar has its own
/// colour. Everything is drawn straight onto the pixel grid of the size asked for, so the icon is sharp at every size.
/// </summary>
public static class TrayIconRenderer
{
    private const double ViewBox = 28;
    private const double RingRadius = 11;
    private const double RingStroke = 2.75;
    private const double DotRadius = 4.5;
    private const double SlashStroke = 2.5;
    private const double SquareCornerRadius = 6;
    private const double ValueFontScale = 0.46;

    /// <summary>
    /// Renders the icon at <paramref name="pixelSize"/> pixels square (96 dpi, so one device pixel per bitmap pixel). With no zone the sensor is not connected and no value is shown.
    /// With a zone and a CO₂ value the icon shows the value.
    /// </summary>
    public static BitmapSource Render(Zone? zone, int pixelSize, double? co2Ppm)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelSize, 1);

        var visual = new DrawingVisual();
        TextOptions.SetTextFormattingMode(visual, TextFormattingMode.Display);

        using (var context = visual.RenderOpen())
        {
            if (zone is not { } known)
            {
                DrawNotConnected(context, pixelSize);
            }
            else if (co2Ppm is { } value)
            {
                DrawValue(context, pixelSize, known, value);
            }
            else
            {
                DrawRing(context, pixelSize, new SolidColorBrush(ZoneColor(known)));
            }
        }

        var bitmap = new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        return bitmap;
    }

    /// <summary>The value as it fits the icon: thousands with one decimal ("0.6k", "1.1k"), whole thousands from 10k. Always a decimal point, whatever the language.</summary>
    public static string FormatValue(double co2Ppm)
    {
        var thousands = Math.Round((decimal)co2Ppm / 1000m, 1, MidpointRounding.AwayFromZero);

        return thousands >= 10
            ? $"{Math.Round(thousands, 0, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture)}k"
            : $"{thousands.ToString("0.0", CultureInfo.InvariantCulture)}k";
    }

    private static void DrawRing(DrawingContext context, int size, Brush brush)
    {
        var scale = size / ViewBox;
        var centre = new Point(size / 2.0, size / 2.0);
        var pen = new Pen(brush, RingStroke * scale);
        pen.Freeze();

        context.DrawEllipse(null, pen, centre, RingRadius * scale, RingRadius * scale);
        context.DrawEllipse(brush, null, centre, DotRadius * scale, DotRadius * scale);
    }

    private static void DrawNotConnected(DrawingContext context, int size)
    {
        var scale = size / ViewBox;
        var grey = new SolidColorBrush(Token("Tray.Offline.Color"));
        var centre = new Point(size / 2.0, size / 2.0);
        var ring = new Pen(grey, RingStroke * scale) { DashStyle = new DashStyle([4 / RingStroke, 3 / RingStroke], 0) };
        var slash = new Pen(grey, SlashStroke * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        context.DrawEllipse(null, ring, centre, RingRadius * scale, RingRadius * scale);
        context.DrawLine(slash, new Point(5 * scale, 23 * scale), new Point(23 * scale, 5 * scale));
    }

    private static void DrawValue(DrawingContext context, int size, Zone zone, double co2Ppm)
    {
        var fill = new SolidColorBrush(ZoneColor(zone));
        var text = new SolidColorBrush(Token(zone == Zone.Elevated ? "Tray.Value.OnAmber.Color" : "Tray.Value.OnLight.Color"));
        var radius = SquareCornerRadius * size / ViewBox;

        context.DrawRoundedRectangle(fill, null, new Rect(0, 0, size, size), radius, radius);

        var label = new FormattedText(
            FormatValue(co2Ppm),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            size * ValueFontScale,
            text,
            1.0);

        context.DrawText(label, new Point((size - label.Width) / 2, (size - label.Height) / 2));
    }

    private static Color ZoneColor(Zone zone) => Token($"Zone.{zone}.Dot.Color");

    private static Color Token(string key) => (Color)Application.Current.FindResource(key);
}
