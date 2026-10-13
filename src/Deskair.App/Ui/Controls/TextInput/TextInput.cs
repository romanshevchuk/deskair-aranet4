using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Deskair.App.Ui.Controls;

/// <summary>A single-line text field in the style of the secondary button: a 1 px border and a focus ring. Enter moves on to the next control, so a value that is saved when the field is left is saved.</summary>
public class TextInput : TextBox
{
    static TextInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TextInput), new FrameworkPropertyMetadata(typeof(TextInput)));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !AcceptsReturn)
        {
            e.Handled = true;
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            return;
        }

        base.OnKeyDown(e);
    }
}
