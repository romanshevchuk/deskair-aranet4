using System.Windows;
using System.Windows.Input;
using Deskair.App.ViewModels.Shell;

namespace Deskair.App.Ui.Views.Shell;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (ShellShortcuts.SectionFor(e.Key, Keyboard.Modifiers) is { } section && DataContext is ShellViewModel shell)
        {
            shell.NavigateCommand.Execute(section);
            e.Handled = true;
        }
        else if (ShellShortcuts.IsRegionSwitch(e.Key, Keyboard.Modifiers))
        {
            // From the header to the page, from anywhere else back to the header.
            (Header.IsKeyboardFocusWithin ? Page : Header).MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            e.Handled = true;
        }
    }
}
