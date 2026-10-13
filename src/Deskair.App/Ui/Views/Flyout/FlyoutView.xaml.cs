using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Deskair.App.Ui.Views.Flyout;

public partial class FlyoutView : UserControl
{
    public FlyoutView()
    {
        InitializeComponent();
    }

    /// <summary>The Snooze button opens its menu above itself; a button does not open a context menu on a left click by itself.</summary>
    private void OnSnoozeClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Top;
            menu.IsOpen = true;
        }
    }
}
