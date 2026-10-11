using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-13) [S, haiku]
// Spec: docs/design/components.md "Hero card", "Stats card"
// Input : Label and value text.
// Output: A caption label (12 px, secondary) above the value (14 px, tabular figures).
// Rules : Text wraps; no fixed width.
// Edge  : A missing value leaves the value line empty.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class KeyValueCell : Control
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(KeyValueCell),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(KeyValueCell),
        new PropertyMetadata(null));

    static KeyValueCell()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(KeyValueCell), new FrameworkPropertyMetadata(typeof(KeyValueCell)));
    }

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
