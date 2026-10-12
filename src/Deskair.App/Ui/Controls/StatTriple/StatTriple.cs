using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

public class StatTriple : ItemsControl
{
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(StatTriple),
        new PropertyMetadata(null));

    static StatTriple()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StatTriple), new FrameworkPropertyMetadata(typeof(StatTriple)));
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }
}
