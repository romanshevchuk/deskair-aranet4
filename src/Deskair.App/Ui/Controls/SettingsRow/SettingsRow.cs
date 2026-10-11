using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-15) [S, haiku]
// Spec: docs/design/screens-and-states.md "Settings"
// Input : Header (label), optional description, content (toggle, segmented control or button).
// Output: Label and description on the left; the content on the right.
// Rules : Content aligns right; the label wraps before it overlaps the content.
// Edge  : No description hides the second line.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class SettingsRow : HeaderedContentControl
{
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(SettingsRow),
        new PropertyMetadata(null));

    static SettingsRow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SettingsRow), new FrameworkPropertyMetadata(typeof(SettingsRow)));
    }

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
