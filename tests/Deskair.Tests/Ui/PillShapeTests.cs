using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Deskair.App.Ui.Controls;
using Deskair.Domain.Zones;

namespace Deskair.Tests.Ui;

/// <summary>A pill ends in half circles. WPF clamps a huge corner radius on each axis separately, which makes elliptical ends, so the radius has to be half the height.</summary>
public class PillShapeTests
{
    private static RenderTargetBitmap Draw(FrameworkElement element, out Size size)
    {
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        size = element.DesiredSize;
        element.Arrange(new Rect(size));
        element.UpdateLayout();

        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);

        return bitmap;
    }

    private static byte Alpha(BitmapSource bitmap, int x, int y) => LayerRenderer.Pixel(bitmap, x, y).A;

    [Theory]
    [InlineData(Zone.Good)]
    [InlineData(Zone.Elevated)]
    [InlineData(Zone.High)]
    public void A_pill_has_semicircular_ends_whatever_its_width(Zone zone)
    {
        UiHost.Run(() =>
        {
            var bitmap = Draw(UiHost.Prepare(new Pill { Content = "▲ 25 ppm vs the week before", Zone = zone }), out var size);
            var radius = size.Height / 2;

            // A point near the left end that a half circle covers and an ellipse as wide as the pill would not.
            Assert.True(Alpha(bitmap, (int)(0.35 * radius), (int)(0.3 * radius)) > 128, "inside the half circle");
            Assert.Equal(0, Alpha(bitmap, 0, 0));
            Assert.Equal(0, Alpha(bitmap, (int)size.Width - 1, (int)size.Height - 1));
            Assert.True(Alpha(bitmap, 1, (int)radius) > 128, "the end reaches the edge in the middle");
        });
    }

    [Fact]
    public void A_short_pill_is_a_circle_ended_stadium_too()
    {
        UiHost.Run(() =>
        {
            var bitmap = Draw(UiHost.Prepare(new Pill { Content = "3" }), out var size);
            var radius = size.Height / 2;

            Assert.True(Alpha(bitmap, (int)(0.35 * radius), (int)(0.3 * radius)) > 128);
        });
    }

    [Fact]
    public void The_toggle_track_is_a_stadium_of_its_own_size()
    {
        UiHost.Run(() =>
        {
            var toggle = UiHost.Prepare(new ToggleSwitch { IsChecked = true });
            var bitmap = Draw(toggle, out var size);
            var radius = size.Height / 2;

            Assert.True(Alpha(bitmap, (int)(0.35 * radius), (int)(0.3 * radius)) > 128, "inside the half circle");
            Assert.Equal(0, Alpha(bitmap, 0, 0));
        });
    }
}
