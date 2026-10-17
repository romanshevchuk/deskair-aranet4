using System.Windows;
using Deskair.App.Ui.Motion;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// The main navigation in the header: text tabs, the current one in primary ink and semibold with a short accent underline that slides to the chosen tab.
/// Items are ordinary list items, so arrow keys move the selection and screen readers announce a selectable list.
/// </summary>
public class NavTabs : SlidingSelector
{
    static NavTabs()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavTabs), new FrameworkPropertyMetadata(typeof(NavTabs)));
    }

    protected override double? FixedIndicatorWidth => Application.Current?.TryFindResource("Size.NavUnderline.Width") as double?;
}
