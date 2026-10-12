using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Deskair.App.Ui.Charts;

namespace Deskair.Tests.Ui;

/// <summary>Draws a chart layer into a bitmap so a test can look at pixels. Call it inside <see cref="UiHost"/>.Run (the brushes come from the theme).</summary>
public static class LayerRenderer
{
    public const int Width = 300;
    public const int Height = 200;

    public static ChartArea Area(double xMin = 0, double xMax = 100, double yMin = 400, double yMax = 2000)
    {
        var area = ChartArea.Create(new Size(Width, Height), new Thickness(40, 10, 10, 30), xMin, xMax, yMin, yMax);

        return area with { Resources = key => Application.Current.TryFindResource(key) };
    }

    public static BitmapSource Render(IChartLayer layer, ChartArea area)
    {
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            layer.Draw(context, area);
        }

        var bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        return bitmap;
    }

    public static Color Pixel(BitmapSource bitmap, int x, int y)
    {
        var pixels = new byte[4];
        bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);

        return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
    }

    public static bool AnyDrawn(BitmapSource bitmap, Int32Rect region)
    {
        var stride = region.Width * 4;
        var pixels = new byte[stride * region.Height];
        bitmap.CopyPixels(region, pixels, stride, 0);

        return pixels.Where((_, index) => index % 4 == 3).Any(alpha => alpha > 0);
    }

    public static bool AnyDrawn(BitmapSource bitmap) => AnyDrawn(bitmap, new Int32Rect(0, 0, Width, Height));

    public static Color Token(string key) => ((SolidColorBrush)Application.Current.FindResource(key)).Color;
}
