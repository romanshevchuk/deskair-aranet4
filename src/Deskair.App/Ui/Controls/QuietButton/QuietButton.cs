using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>A button with no fill and no border until the pointer is over it: for the quiet actions in a band (flyout Snooze). Content is free, so it can hold an icon, a word and an arrow.</summary>
public class QuietButton : Button
{
    static QuietButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(QuietButton), new FrameworkPropertyMetadata(typeof(QuietButton)));
    }
}
