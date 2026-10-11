using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-06) [S, haiku]
// Spec: docs/design/components.md "Empty / onboarding card"; docs/design/screens-and-states.md "Live conditions"
// Input : Icon, title, message; buttons arrive as content.
// Output: A centred Card with the icon, title (16 px, 600), message and the content below.
// Rules : Built on the Card control.
// Edge  : No icon hides the icon area.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class EmptyStateCard : ContentControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(EmptyStateCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(EmptyStateCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(object),
        typeof(EmptyStateCard),
        new PropertyMetadata(null));

    static EmptyStateCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(EmptyStateCard), new FrameworkPropertyMetadata(typeof(EmptyStateCard)));
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public object? Icon
    {
        get => (object?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
