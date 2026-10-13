using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>A single-line text field in the style of the secondary button: a 1 px border and a focus ring.</summary>
public class TextInput : TextBox
{
    static TextInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TextInput), new FrameworkPropertyMetadata(typeof(TextInput)));
    }
}
