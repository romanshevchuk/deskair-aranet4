using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-17) [S, haiku]
// Spec: docs/design/screens-and-states.md "Screens" (Snooze, Settings buttons)
// Input : Content (text).
// Output: An outlined button: Surface background, Border outline, hover, pressed, disabled and focus states.
// Rules : Prototype: Text.Strong2 text, 1 px Border outline, radius 6. Same shape and height as PrimaryButton.
// Edge  : Disabled state at 50 % opacity.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class SecondaryButton : Button
{
    static SecondaryButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SecondaryButton), new FrameworkPropertyMetadata(typeof(SecondaryButton)));
    }
}
