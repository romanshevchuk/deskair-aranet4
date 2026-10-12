using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Connection;
using Deskair.Services.Messages;
using Deskair.Services.Platform;

namespace Deskair.App.ViewModels.Connection;

public sealed partial class ConnectionViewModel : ObservableObject
{
    private readonly IMessenger? messenger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStaleBanner))]
    [NotifyPropertyChangedFor(nameof(ShowLowBatteryBanner))]
    [NotifyPropertyChangedFor(nameof(ShowOfflineView))]
    [NotifyPropertyChangedFor(nameof(ShowFirstRunView))]
    private ConnectionState state;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLowBatteryBanner))]
    private bool lowBatteryDismissed;

    public ConnectionViewModel()
    {
    }

    /// <summary>Follows <see cref="ConnectionStateChanged"/> messages, on the UI thread, and can ask for a retry.</summary>
    public ConnectionViewModel(IMessenger messenger, IUiDispatcher dispatcher)
    {
        this.messenger = messenger;
        messenger.Register<ConnectionViewModel, ConnectionStateChanged>(this, (self, message) => dispatcher.Post(() => self.Update(message.State)));
    }

    public bool ShowStaleBanner => State == ConnectionState.Stale;

    public bool ShowLowBatteryBanner => State == ConnectionState.LowBattery && !LowBatteryDismissed;

    public bool ShowOfflineView => State == ConnectionState.NotConnected;

    public bool ShowFirstRunView => State == ConnectionState.FirstRun;

    public void Update(ConnectionState newState)
    {
        if (newState != State)
        {
            LowBatteryDismissed = false;
        }

        State = newState;
    }

    [RelayCommand]
    private void Retry()
    {
        messenger?.Send(new RetryRequested());
    }

    [RelayCommand]
    private void DismissLowBattery()
    {
        LowBatteryDismissed = true;
    }
}
