using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>
/// The night hours (Settings → Sleep): when the night starts and ends, in local time. The Night filter in History and the sleep card use them. A time that does not parse is
/// flagged, and so is a night that would start when it ends (<see cref="IsEmptyWindow"/>); neither is saved. A change from the screen is saved at once.
/// </summary>
public sealed partial class SleepSettingsViewModel : ObservableObject
{
    private readonly SettingsService settings;
    private readonly ILogger<SleepSettingsViewModel> logger;

    /// <summary>True when the start and the end are the same time, which leaves no night.</summary>
    [ObservableProperty]
    private bool isEmptyWindow;

    public SleepSettingsViewModel(SettingsService settings, ILogger<SleepSettingsViewModel> logger)
    {
        this.settings = settings;
        this.logger = logger;

        NightStart.ValueChanged += (_, _) => ApplyLater();
        NightEnd.ValueChanged += (_, _) => ApplyLater();
    }

    public TimeField NightStart { get; } = new();

    public TimeField NightEnd { get; } = new();

    public void Load()
    {
        NightStart.Load(settings.Current.NightStart);
        NightEnd.Load(settings.Current.NightEnd);
        IsEmptyWindow = false;
    }

    private void ApplyLater()
    {
        IsEmptyWindow = NightStart.Value == NightEnd.Value;

        if (!IsEmptyWindow)
        {
            _ = ApplyQuietlyAsync();
        }
    }

    private async Task ApplyQuietlyAsync()
    {
        try
        {
            await settings.UpdateAsync(current => current with { NightStart = NightStart.Value, NightEnd = NightEnd.Value }, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Saving the night hours failed.");
            }
        }
    }
}
