using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

/// <summary>A small rounded label in zone colours (or neutral without a zone).</summary>
public class Pill : ContentControl
{
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(Pill),
        new PropertyMetadata(null));

    static Pill()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Pill), new FrameworkPropertyMetadata(typeof(Pill)));
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }
}
