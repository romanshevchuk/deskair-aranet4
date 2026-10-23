using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Platform;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class StartupSettingsViewModel : ObservableObject
{
    private readonly IStartupRegistration registration;
    private readonly ILogger<StartupSettingsViewModel> logger;
    private bool applying;

    [ObservableProperty]
    private bool startWithWindows;

    public StartupSettingsViewModel(IStartupRegistration registration, ILogger<StartupSettingsViewModel> logger)
    {
        this.registration = registration;
        this.logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        SetWithoutApplying(await registration.IsEnabledAsync(cancellationToken));
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (!applying)
        {
            _ = ApplyAsync(value);
        }
    }

    private void SetWithoutApplying(bool value)
    {
        applying = true;

        try
        {
            StartWithWindows = value;
        }
        finally
        {
            applying = false;
        }
    }

    private async Task ApplyAsync(bool enabled)
    {
        try
        {
            await registration.SetEnabledAsync(enabled, CancellationToken.None);
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Changing start with Windows to {Enabled} failed.", enabled);
            }

            SetWithoutApplying(!enabled);
        }
    }
}
