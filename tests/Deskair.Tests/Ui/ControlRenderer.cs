using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Deskair.Tests.Ui;

/// <summary>Draws an element to a PNG with the window background and text defaults, so a visual check does not need a screen. Call it on the UI thread (inside <see cref="UiHost"/>.Run).</summary>
public static class ControlRenderer
{
    private const double Scale = 2;

    public static byte[] ToPng(FrameworkElement content, double width, double? height = null)
    {
        var application = Application.Current;
        var host = new Border
        {
            Width = width,
            Height = height ?? double.NaN,
            Background = (Brush)application.FindResource("Window.Bg.Brush"),
            Child = content,
            UseLayoutRounding = true
        };

        TextElement.SetForeground(host, (Brush)application.FindResource("Text.Primary.Brush"));
        TextElement.SetFontFamily(host, (FontFamily)application.FindResource("Font.Family"));
        TextElement.SetFontSize(host, (double)application.FindResource("Font.Size.Body"));

        host.Measure(new Size(width, height ?? double.PositiveInfinity));
        host.Arrange(new Rect(host.DesiredSize));
        host.UpdateLayout();

        var pixelWidth = (int)Math.Ceiling(host.ActualWidth * Scale);
        var pixelHeight = (int)Math.Ceiling(host.ActualHeight * Scale);
        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96 * Scale, 96 * Scale, PixelFormats.Pbgra32);
        bitmap.Render(host);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);

        host.Child = null;

        return stream.ToArray();
    }
}
