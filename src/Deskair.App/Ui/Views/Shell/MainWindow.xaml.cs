using System.Windows;
using Deskair.App.ViewModels.Shell;

namespace Deskair.App.Ui.Views.Shell;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
