using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Services.Connection;
using Deskair.Services.Messages;
using Deskair.Services.Platform;

namespace Deskair.App.ViewModels.Connection;

public sealed partial class ConnectionViewModel : RetryingViewModel
{
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
        : base(null, null, null)
    {
    }

    /// <summary>Follows <see cref="ConnectionStateChanged"/> messages, on the UI thread, and can ask for a retry.</summary>
    public ConnectionViewModel(IMessenger messenger, IUiDispatcher dispatcher, ILocalizer localizer)
        : base(messenger, dispatcher, localizer)
    {
        messenger.Register<ConnectionViewModel, ConnectionStateChanged>(this, (self, message) => dispatcher.Post(() => self.Update(message.State)));
        messenger.Register<ConnectionViewModel, SensorStatusUpdated>(this, (self, _) => dispatcher.Post(self.EndRetry));
        messenger.Register<ConnectionViewModel, DeviceSelected>(this, (self, _) => dispatcher.Post(self.EndRetry));
        messenger.Register<ConnectionViewModel, LanguageChanged>(this, (self, _) => dispatcher.Post(self.RefreshRetryText));
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
    private void DismissLowBattery()
    {
        LowBatteryDismissed = true;
    }
}
