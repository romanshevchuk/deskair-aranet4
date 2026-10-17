using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// The main navigation in the header: text tabs, the current one in primary ink and semibold with a short accent underline. Items are ordinary list items,
/// so arrow keys move the selection and screen readers announce a selectable list.
/// </summary>
public class NavTabs : ListBox
{
    static NavTabs()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavTabs), new FrameworkPropertyMetadata(typeof(NavTabs)));
    }
}
