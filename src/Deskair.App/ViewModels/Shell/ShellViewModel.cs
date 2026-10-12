using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Live;

namespace Deskair.App.ViewModels.Shell;

/// <summary>State of the main window: which section is shown, and the view models of the parts it shows.</summary>
public sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private ShellSection section = ShellSection.Live;

    [ObservableProperty]
    private LiveViewModel? live;

    [ObservableProperty]
    private ConnectionViewModel? connection;

    [ObservableProperty]
    private FooterViewModel? footer;

    [ObservableProperty]
    private RoomChipViewModel? room;

    [RelayCommand]
    private void Navigate(ShellSection target)
    {
        Section = target;
    }
}
