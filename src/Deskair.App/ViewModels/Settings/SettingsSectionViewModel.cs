using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>Base for the settings sections that save a change at once. A save changes only the fields the caller names, so a value changed elsewhere (the tray menu) is never written back old.</summary>
public abstract class SettingsSectionViewModel : ObservableObject
{
    private readonly ILogger logger;

    protected SettingsSectionViewModel(SettingsService settings, ILogger logger)
    {
        Settings = settings;
        this.logger = logger;
    }

    protected SettingsService Settings { get; }

    /// <summary>Saves in the background and logs a warning if it fails.</summary>
    protected void SaveInBackground(Func<AppSettings, AppSettings> change, string failureMessage)
    {
        _ = SaveQuietlyAsync(change, failureMessage);
    }

    private async Task SaveQuietlyAsync(Func<AppSettings, AppSettings> change, string failureMessage)
    {
        try
        {
            await Settings.UpdateAsync(change, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "{Message}", failureMessage);
            }
        }
    }
}
