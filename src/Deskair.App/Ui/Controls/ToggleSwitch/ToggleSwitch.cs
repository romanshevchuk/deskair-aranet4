using System.Windows;
using System.Windows.Controls.Primitives;

namespace Deskair.App.Ui.Controls;

/// <summary>An on/off switch (38 × 22 px). The optional content is its label.</summary>
public class ToggleSwitch : ToggleButton
{
    static ToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
    }
}
