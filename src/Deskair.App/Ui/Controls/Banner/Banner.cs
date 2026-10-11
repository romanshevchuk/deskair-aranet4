using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-05) [S, haiku]
// Spec: docs/design/components.md "Banner"; docs/design/screens-and-states.md "Live conditions"
// Input : Message, optional action (text and command), optional dismiss command.
// Output: A Border row with radius 12: message left; action button and dismiss button right.
// Rules : Colours: Zone.Elevated advice tokens until the banner colours are decided (questions.md Q15).
// Edge  : No action and no dismiss shows just the message.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class Banner : ContentControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(Banner),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ActionTextProperty = DependencyProperty.Register(
        nameof(ActionText),
        typeof(string),
        typeof(Banner),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ActionCommandProperty = DependencyProperty.Register(
        nameof(ActionCommand),
        typeof(ICommand),
        typeof(Banner),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DismissCommandProperty = DependencyProperty.Register(
        nameof(DismissCommand),
        typeof(ICommand),
        typeof(Banner),
        new PropertyMetadata(null));

    static Banner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Banner), new FrameworkPropertyMetadata(typeof(Banner)));
    }

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? ActionText
    {
        get => (string?)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }
}
