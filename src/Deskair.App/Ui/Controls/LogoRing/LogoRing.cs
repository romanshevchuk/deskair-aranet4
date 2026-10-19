using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>The app logo in the window header: a ring with a centre dot in the brand ink. Only the tray icon changes colour with the zone.</summary>
public class LogoRing : Control
{
    static LogoRing()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(LogoRing), new FrameworkPropertyMetadata(typeof(LogoRing)));
    }
}
