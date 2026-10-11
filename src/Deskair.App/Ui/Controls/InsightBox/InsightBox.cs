using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-12) [S, haiku]
// Spec: docs/design/components.md "Stats card" (insight); docs/design/design-tokens.md "Neutrals" (insight.bg)
// Input : Content (usually text).
// Output: A panel with Insight.Bg, radius 10 and Secondary text.
// Rules : No border.
// Edge  : Empty content collapses the box.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class InsightBox : ContentControl
{
    static InsightBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(InsightBox), new FrameworkPropertyMetadata(typeof(InsightBox)));
    }
}
