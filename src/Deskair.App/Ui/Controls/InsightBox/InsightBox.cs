using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// A panel for one sentence of insight. With a <see cref="Zone"/> it takes the advice colours of that zone; without one it is a quiet neutral panel. The sentence may start with
/// a bold lead (<c>**Worst hours: 15:00–18:00** (avg ...)</c>, see <see cref="Markup.InlineMarkup"/>). Empty content collapses it.
/// </summary>
public class InsightBox : ContentControl
{
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(InsightBox),
        new PropertyMetadata(null));

    static InsightBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(InsightBox), new FrameworkPropertyMetadata(typeof(InsightBox)));
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }
}
