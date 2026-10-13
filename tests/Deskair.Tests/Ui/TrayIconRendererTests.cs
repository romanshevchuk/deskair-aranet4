using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Deskair.App.Platform.Tray;
using Deskair.Domain.Zones;
using Deskair.Services.Settings;

namespace Deskair.Tests.Ui;

public class TrayIconRendererTests
{
    private static readonly Color good = Color.FromRgb(0x1E, 0x8A, 0x5C);
    private static readonly Color elevated = Color.FromRgb(0xD4, 0x96, 0x1A);
    private static readonly Color high = Color.FromRgb(0xC7, 0x45, 0x3A);

    [Theory]
    [InlineData(0, "0.0k")]
    [InlineData(600, "0.6k")]
    [InlineData(949, "0.9k")]
    [InlineData(1068, "1.1k")]
    [InlineData(1700, "1.7k")]
    [InlineData(9949, "9.9k")]
    [InlineData(9950, "10k")]
    [InlineData(12400, "12k")]
    public void The_value_is_shortened_to_thousands(double ppm, string expected)
    {
        Assert.Equal(expected, TrayIconRenderer.FormatValue(ppm));
    }

    [Fact]
    public void The_text_is_the_same_in_every_language()
    {
        // A decimal comma would not fit the icon: the shortened value always uses a point.
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("uk-UA");

        try
        {
            Assert.Equal("1.1k", TrayIconRenderer.FormatValue(1068));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(32)]
    public void The_icon_has_the_size_asked_for_at_96_dpi(int size)
    {
        var bitmap = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(Zone.Good, size, null));

        Assert.Equal((size, size), (bitmap.PixelWidth, bitmap.PixelHeight));
        Assert.Equal((96.0, 96.0), (bitmap.DpiX, bitmap.DpiY));
        Assert.True(bitmap.IsFrozen);
    }

    [Theory]
    [InlineData(Zone.Good)]
    [InlineData(Zone.Elevated)]
    [InlineData(Zone.High)]
    public void The_ring_and_its_centre_dot_use_the_zone_colour(Zone zone)
    {
        var expected = zone switch { Zone.Good => good, Zone.Elevated => elevated, _ => high };

        var bitmap = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(zone, 32, null));

        Assert.Equal(expected, Opaque(bitmap, 16, 16));
        Assert.Equal(0, PixelAt(bitmap, 0, 0).A);
    }

    [Fact]
    public void Between_the_ring_and_the_dot_the_icon_is_transparent()
    {
        var bitmap = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(Zone.Good, 32, null));

        Assert.Equal(0, PixelAt(bitmap, 16, 9).A);
    }

    [Fact]
    public void The_icon_does_not_depend_on_the_app_theme()
    {
        var light = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(Zone.Elevated, 24, null));
        var dark = UiHost.Run(ThemeChoice.Dark, () => TrayIconRenderer.Render(Zone.Elevated, 24, null));

        Assert.Equal(Pixels(light), Pixels(dark));
    }

    [Fact]
    public void Not_connected_is_a_grey_ring_without_the_zone_colours()
    {
        var bitmap = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(null, 32, null));

        var centre = PixelAt(bitmap, 16, 16);
        Assert.True(centre.A > 0, "the slash crosses the centre");
        Assert.NotEqual(good, Color.FromRgb(centre.R, centre.G, centre.B));
        Assert.Equal(0, PixelAt(bitmap, 0, 0).A);
        Assert.InRange(Math.Abs(centre.R - centre.G), 0, 20);
    }

    [Theory]
    [InlineData(Zone.Good)]
    [InlineData(Zone.Elevated)]
    [InlineData(Zone.High)]
    public void With_a_value_the_icon_is_a_rounded_square_in_the_zone_colour(Zone zone)
    {
        var expected = zone switch { Zone.Good => good, Zone.Elevated => elevated, _ => high };

        var bitmap = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(zone, 32, 1068));

        Assert.Equal(expected, Opaque(bitmap, 1, 16));
        Assert.Equal(0, PixelAt(bitmap, 0, 0).A);
    }

    [Fact]
    public void The_value_text_is_dark_on_amber_and_white_on_the_other_zones()
    {
        var onAmber = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(Zone.Elevated, 32, 1068));
        var onGreen = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(Zone.Good, 32, 600));

        Assert.Contains(Pixels(onAmber).Chunk(4), pixel => pixel[2] < 0x60 && pixel[1] < 0x60 && pixel[0] < 0x60 && pixel[3] == 255);
        Assert.Contains(Pixels(onGreen).Chunk(4), pixel => pixel is [> 0xE0, > 0xE0, > 0xE0, 255]);
    }

    [Fact]
    public void Without_a_zone_a_value_is_not_shown()
    {
        var withValue = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(null, 32, 1068));
        var without = UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(null, 32, null));

        Assert.Equal(Pixels(without), Pixels(withValue));
    }

    [Fact]
    public void A_size_that_is_not_positive_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UiHost.Run(ThemeChoice.Light, () => TrayIconRenderer.Render(Zone.Good, 0, null)));
    }

    private static byte[] Pixels(BitmapSource bitmap)
    {
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

        return pixels;
    }

    private static (byte B, byte G, byte R, byte A) PixelAt(BitmapSource bitmap, int x, int y)
    {
        var pixels = new byte[4];
        bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);

        return (pixels[0], pixels[1], pixels[2], pixels[3]);
    }

    /// <summary>The colour of a fully opaque pixel; fails when the pixel is see-through or half covered.</summary>
    private static Color Opaque(BitmapSource bitmap, int x, int y)
    {
        var pixel = PixelAt(bitmap, x, y);
        Assert.Equal(255, pixel.A);

        return Color.FromRgb(pixel.R, pixel.G, pixel.B);
    }

    [Fact]
    public void Render_the_icon_sheet()
    {
        RenderOutput.Require();

        var png = UiHost.Run(ThemeChoice.Light, () => IconSheet.ToPng());
        var folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ThisFile())!, "..", "..", "..", "artifacts", "visual"));
        System.IO.Directory.CreateDirectory(folder);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, "tray-icons.png"), png);
    }

    private static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

    /// <summary>Every icon at 16, 20, 24 and 32 px on a light and a dark taskbar, as drawn (left) and magnified 8 times without smoothing (right).</summary>
    private static class IconSheet
    {
        private static readonly int[] sizes = [16, 20, 24, 32];

        public static byte[] ToPng()
        {
            var kinds = new (string Name, Zone? Zone, double? Value)[]
            {
                ("good", Zone.Good, null), ("elevated", Zone.Elevated, null), ("high", Zone.High, null), ("offline", null, null),
                ("good 0.6k", Zone.Good, 600), ("elevated 1.1k", Zone.Elevated, 1068), ("high 1.7k", Zone.High, 1700)
            };
            const int zoom = 8;
            const int pad = 10;
            var rowHeight = (32 * zoom) + pad;
            var width = 1700;
            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                var y = 0;

                foreach (var background in new[] { Colors.White, Color.FromRgb(0x20, 0x20, 0x20) })
                {
                    var brush = new SolidColorBrush(background);
                    context.DrawRectangle(brush, null, new Rect(0, y, width, rowHeight * kinds.Length));

                    foreach (var (name, zone, value) in kinds)
                    {
                        var x = pad;
                        var label = new FormattedText(name, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, new SolidColorBrush(background == Colors.White ? Colors.Black : Colors.White), 1.0);
                        context.DrawText(label, new Point(x, y + pad));
                        x += 150;

                        foreach (var size in sizes)
                        {
                            var icon = TrayIconRenderer.Render(zone, size, value);
                            context.DrawImage(icon, new Rect(x, y + pad, size, size));
                            x += size + pad;
                        }

                        x += 20;

                        foreach (var size in sizes)
                        {
                            var icon = TrayIconRenderer.Render(zone, size, value);
                            var big = new ImageDrawing(icon, new Rect(x, y + pad, size * zoom / 2, size * zoom / 2));
                            var group = new DrawingGroup();
                            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.NearestNeighbor);
                            using (var inner = group.Open())
                            {
                                inner.DrawDrawing(big);
                            }

                            context.DrawDrawing(group);
                            x += (size * zoom / 2) + pad;
                        }

                        y += rowHeight;
                    }
                }
            }

            var height = rowHeight * kinds.Length * 2;
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new System.IO.MemoryStream();
            encoder.Save(stream);

            return stream.ToArray();
        }
    }
}
