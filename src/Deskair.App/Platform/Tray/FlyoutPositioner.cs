using System.Windows;

namespace Deskair.App.Platform.Tray;

/// <summary>
/// Works out where the tray flyout goes. Everything is in device-independent units. The work area is the part of the monitor the taskbar leaves free,
/// so the side the icon lies outside of tells where the taskbar is; the flyout goes on the side of the icon that faces the screen, a small gap from the taskbar,
/// and is moved just enough to stay fully inside the work area.
/// </summary>
public static class FlyoutPositioner
{
    /// <summary>Space between the taskbar edge and the flyout.</summary>
    public const double Gap = 8;

    /// <summary>
    /// The top-left point for the flyout. An icon outside the work area is on the taskbar: the flyout goes next to that edge and is centred on the icon along it.
    /// An icon inside the work area (the overflow pop-up) gets the flyout above it, or below it when there is no room above. A flyout larger than the work area is pinned to its top left.
    /// </summary>
    public static Point Position(Rect trayIcon, Size flyout, Rect workArea)
    {
        var centreX = trayIcon.Left + (trayIcon.Width / 2);
        var centreY = trayIcon.Top + (trayIcon.Height / 2);
        double x;
        double y;

        if (trayIcon.Top >= workArea.Bottom)
        {
            // Taskbar at the bottom.
            x = centreX - (flyout.Width / 2);
            y = workArea.Bottom - flyout.Height - Gap;
        }
        else if (trayIcon.Bottom <= workArea.Top)
        {
            // Taskbar at the top.
            x = centreX - (flyout.Width / 2);
            y = workArea.Top + Gap;
        }
        else if (trayIcon.Right <= workArea.Left)
        {
            // Taskbar at the left.
            x = workArea.Left + Gap;
            y = centreY - (flyout.Height / 2);
        }
        else if (trayIcon.Left >= workArea.Right)
        {
            // Taskbar at the right.
            x = workArea.Right - flyout.Width - Gap;
            y = centreY - (flyout.Height / 2);
        }
        else
        {
            // The icon is not on the taskbar (the overflow pop-up): above it when it fits, else below.
            x = centreX - (flyout.Width / 2);
            y = trayIcon.Top - flyout.Height - Gap >= workArea.Top ? trayIcon.Top - flyout.Height - Gap : trayIcon.Bottom + Gap;
        }

        return new Point(Clamp(x, workArea.Left, workArea.Right - flyout.Width), Clamp(y, workArea.Top, workArea.Bottom - flyout.Height));
    }

    /// <summary>Keeps a value in the range; when the range is empty (the flyout is larger than the space) the low end wins, so the top left stays visible.</summary>
    private static double Clamp(double value, double low, double high)
    {
        return high < low ? low : Math.Min(Math.Max(value, low), high);
    }
}
