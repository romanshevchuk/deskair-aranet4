using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

/// <summary>The app logo: a ring with a centre dot in the colour of the current zone (grey without a zone). The same ring is the tray icon.</summary>
public class LogoRing : Control
{
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(LogoRing),
        new PropertyMetadata(null));

    static LogoRing()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(LogoRing), new FrameworkPropertyMetadata(typeof(LogoRing)));
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }
}
