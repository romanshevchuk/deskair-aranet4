using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// The zone's icon without a disc: a ring with a tick (Good), a square with a cross (Elevated), a triangle with a mark (High), in the zone's advice colour. The size is the control's own
/// width and height. No zone draws nothing, so the icon is never the only carrier of the zone: the text next to it names it too.
/// </summary>
public class ZoneIcon : Control
{
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(ZoneIcon),
        new PropertyMetadata(null));

    static ZoneIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoneIcon), new FrameworkPropertyMetadata(typeof(ZoneIcon)));
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }
}
