using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.History;
using Deskair.App.ViewModels.Live;
using Deskair.App.ViewModels.Onboarding;
using Deskair.App.ViewModels.Settings;
using Deskair.Domain.Zones;
using Deskair.Services.Connection;
using Deskair.Services.Messages;

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

    /// <summary>The banner shown while the saved readings cannot be read.</summary>
    [ObservableProperty]
    private HistoryAccessViewModel? historyAccess;

    [ObservableProperty]
    private RoomChipViewModel? room;

    /// <summary>First run: the welcome screen that takes the place of Live while there is no sensor.</summary>
    [ObservableProperty]
    private OnboardingViewModel? onboarding;

    /// <summary>Not connected: the full-screen "can't reach your sensor" view that takes the place of Live.</summary>
    [ObservableProperty]
    private OfflineViewModel? offline;

    [ObservableProperty]
    private SettingsViewModel? settings;

    /// <summary>History: the toolbar, the charts and the stats. It loads when the section is opened.</summary>
    [ObservableProperty]
    private HistoryViewModel? history;

    /// <summary>The zone of the header's logo ring and room chip: the current air, or none (grey) while there is no sensor to read (not connected, first run).</summary>
    [ObservableProperty]
    private Zone? headerZone;

    partial void OnConnectionChanged(ConnectionViewModel? value)
    {
        if (value is not null)
        {
            value.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ConnectionViewModel.State))
                {
                    UpdateHeaderZone();
                }
            };
        }

        UpdateHeaderZone();
    }

    partial void OnLiveChanged(LiveViewModel? value)
    {
        if (value is not null)
        {
            value.Hero.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(HeroViewModel.Zone))
                {
                    UpdateHeaderZone();
                }
            };
        }

        UpdateHeaderZone();
    }

    private void UpdateHeaderZone()
    {
        HeaderZone = Connection?.State is ConnectionState.NotConnected or ConnectionState.FirstRun ? null : Live?.Hero.Zone;
    }
    public ShellViewModel()
    {
    }

    /// <summary>
    /// Also follows <see cref="AddSensorRequested"/> (goes to Live and opens the sensor search there) and <see cref="ImportRequested"/> (goes to Settings,
    /// where the import is).
    /// </summary>
    public ShellViewModel(IMessenger messenger)
    {
        messenger.Register<ShellViewModel, AddSensorRequested>(this, static (self, _) => self.OnAddSensor());
        messenger.Register<ShellViewModel, ImportRequested>(this, static (self, _) => self.Section = ShellSection.Settings);
    }

    [RelayCommand]
    private void Navigate(ShellSection target)
    {
        Section = target;
    }

    partial void OnSectionChanged(ShellSection value)
    {
        if (value == ShellSection.History)
        {
            History?.Refresh();
        }
    }

    private void OnAddSensor()
    {
        Section = ShellSection.Live;
        Onboarding?.OpenToAddSensor();
    }
}
