using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Live;
using Deskair.App.ViewModels.Onboarding;
using Deskair.App.ViewModels.Settings;

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

    /// <summary>First run: the welcome screen that takes the place of Live while there is no sensor.</summary>
    [ObservableProperty]
    private OnboardingViewModel? onboarding;

    [ObservableProperty]
    private SettingsViewModel? settings;

    [RelayCommand]
    private void Navigate(ShellSection target)
    {
        Section = target;
    }
}
