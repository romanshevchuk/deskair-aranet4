using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-14) [S, haiku]
// Spec: docs/design/screens-and-states.md "Settings"
// Input : Header text and rows as content.
// Output: A Card with the header as a section label and rows separated by Divider lines.
// Rules : Built on the Card control; rows are SettingsRow.
// Edge  : No header hides the header line.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class SettingsCard : HeaderedContentControl
{
    static SettingsCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SettingsCard), new FrameworkPropertyMetadata(typeof(SettingsCard)));
    }
}
