using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Deskair.App.ViewModels.Shell;

/// <summary>State of the main window: which section is shown.</summary>
public sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private ShellSection section = ShellSection.Live;

    [RelayCommand]
    private void Navigate(ShellSection target)
    {
        Section = target;
    }
}
