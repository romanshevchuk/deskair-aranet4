using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

/// <summary>A small dot in the zone colour. Without a zone it is neutral grey (for example "no data").</summary>
public class StatusDot : Control
{
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(StatusDot),
        new PropertyMetadata(null));

    static StatusDot()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusDot), new FrameworkPropertyMetadata(typeof(StatusDot)));
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }
}
