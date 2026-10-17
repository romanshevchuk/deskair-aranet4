using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>
/// The night hours (Settings → Sleep): when the night starts and ends, in local time. The Night filter in History and the sleep card use them. A time that does not parse is
/// flagged, and so is a night that would start when it ends (<see cref="IsEmptyWindow"/>); neither is saved. A change from the screen is saved at once.
/// </summary>
public sealed partial class SleepSettingsViewModel : SettingsSectionViewModel
{
    /// <summary>True when the start and the end are the same time, which leaves no night.</summary>
    [ObservableProperty]
    private bool isEmptyWindow;

    public SleepSettingsViewModel(SettingsService settings, ILogger<SleepSettingsViewModel> logger)
        : base(settings, logger)
    {
        NightStart.ValueChanged += (_, _) => ApplyLater();
        NightEnd.ValueChanged += (_, _) => ApplyLater();
    }

    public TimeField NightStart { get; } = new();

    public TimeField NightEnd { get; } = new();

    public void Load()
    {
        NightStart.Load(Settings.Current.NightStart);
        NightEnd.Load(Settings.Current.NightEnd);
        IsEmptyWindow = false;
    }

    private void ApplyLater()
    {
        IsEmptyWindow = NightStart.Value == NightEnd.Value;

        if (!IsEmptyWindow)
        {
            SaveInBackground(current => current with { NightStart = NightStart.Value, NightEnd = NightEnd.Value }, "Saving the night hours failed.");
        }
    }
}
