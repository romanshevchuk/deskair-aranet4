using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-16) [S, haiku]
// Spec: docs/design/screens-and-states.md "Screens" (filled Open Deskair button); docs/design/accessibility.md
// Input : Content (text).
// Output: A filled button with hover, pressed, disabled and keyboard focus states.
// Rules : Prototype: fill Text.Primary, white text, radius 6 (Radius.Button), padding 8 / 14, weight 600.
// Edge  : Disabled state at 50 % opacity.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class PrimaryButton : Button
{
    static PrimaryButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PrimaryButton), new FrameworkPropertyMetadata(typeof(PrimaryButton)));
    }
}
