using System.Windows.Media.Imaging;
using Deskair.Domain.Zones;

namespace Deskair.App.Platform.Tray;

public static class TrayIconRenderer
{
    // TODO(TRAY-01b) [L, sonnet]
    // Spec: docs/design/screens-and-states.md "Tray icon states"; docs/design/design-tokens.md "Icons"; spike S4.
    // Input : zone (null = not connected), pixel size (16, 20, 24 or 32), whether to show the value.
    // Output: a bitmap of the ring in the zone colour, or a rounded square with the value (0.6k, 1.1k).
    // Rules : 28 x 28 viewbox, circle r 11, stroke 2.75 to 3, centre dot r 4.5; colours from the Zone tokens.
    // Edge  : not connected draws a grey dashed ring with a slash.
    // Done  : manual check that icons are crisp at 100 to 200 % DPI (spike S4).
    public static BitmapSource Render(Zone? zone, int pixelSize, double? co2Ppm)
        => throw new NotImplementedException("TODO(TRAY-01b)");
}
