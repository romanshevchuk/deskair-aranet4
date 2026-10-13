using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Services.Messages;
using Deskair.Services.Platform;

namespace Deskair.App.ViewModels.Connection;

/// <summary>
/// The "Try again" button of the stale banner and the offline view, and what it says about the attempt: "Try again", then "Trying…" (the button is off until there is an answer),
/// then "Still can't reach it. Try again" when the sensor session reports another failed attempt. A connected sensor, or another sensor being chosen, ends it (the derived view
/// models call <see cref="EndRetry"/> and <see cref="RefreshRetryText"/> from the handlers they already have, because a recipient can register for a message only once).
/// </summary>
public abstract partial class RetryingViewModel : ObservableObject
{
    private readonly IMessenger? messenger;
    private readonly ILocalizer? localizer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RetryText))]
    [NotifyCanExecuteChangedFor(nameof(RetryCommand))]
    private bool isRetrying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RetryText))]
    private bool retryFailed;

    protected RetryingViewModel(IMessenger? messenger, IUiDispatcher? dispatcher, ILocalizer? localizer)
    {
        this.messenger = messenger;
        this.localizer = localizer;

        if (messenger is null || dispatcher is null)
        {
            return;
        }

        messenger.Register<RetryingViewModel, SensorConnectionFailed>(this, (self, _) => dispatcher.Post(self.OnConnectionFailed));
    }

    /// <summary>The label of the retry button.</summary>
    public string RetryText => localizer?.Get(IsRetrying ? "Action_Retrying" : RetryFailed ? "Action_RetryFailed" : "Action_Retry") ?? string.Empty;

    [RelayCommand(CanExecute = nameof(CanRetry))]
    private void Retry()
    {
        RetryFailed = false;
        IsRetrying = true;
        messenger?.Send(new RetryRequested());
    }

    /// <summary>Call when the language changed, so the button text is looked up again. On the UI thread.</summary>
    protected void RefreshRetryText() => OnPropertyChanged(nameof(RetryText));

    private bool CanRetry() => !IsRetrying;

    private void OnConnectionFailed()
    {
        IsRetrying = false;
        RetryFailed = true;
    }

    /// <summary>Call when a sensor connected or another sensor was chosen: the attempt is over. On the UI thread.</summary>
    protected void EndRetry()
    {
        IsRetrying = false;
        RetryFailed = false;
    }
}
