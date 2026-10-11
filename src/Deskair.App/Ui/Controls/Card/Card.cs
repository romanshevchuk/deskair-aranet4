using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// A rounded surface. With a <see cref="Zone"/> it takes the zone tint and a 1.5 px zone border.
/// This is a reference control: new controls follow its pattern (docs/guides/add-a-control.md).
/// </summary>
public class Card : ContentControl
{
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(Card),
        new PropertyMetadata(null));

    static Card()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Card), new FrameworkPropertyMetadata(typeof(Card)));
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }
}
