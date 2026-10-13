using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;

namespace Deskair.Services.Settings;

/// <summary>User preferences. The full list is still open (see docs/plans/questions.md Q6).</summary>
public sealed record AppSettings(
    LanguageChoice Language,
    ThemeChoice Theme,
    bool AlertsEnabled)
{
    public TemperatureUnit TemperatureUnit { get; init; } = TemperatureUnit.Celsius;

    /// <summary>The device shown on launch; null until the user picks one.</summary>
    public DeviceId? SelectedDevice { get; init; }

    public bool NotifyAtElevated { get; init; } = true;

    public bool NotifyAtHigh { get; init; } = true;

    public bool QuietHoursEnabled { get; init; }

    public TimeOnly QuietHoursStart { get; init; } = new(22, 0);

    public TimeOnly QuietHoursEnd { get; init; } = new(7, 0);

    /// <summary>The tray icon shows the CO₂ value ("1.1k") instead of the ring (Settings, or the tray menu).</summary>
    public bool ShowValueOnTrayIcon { get; init; }

    /// <summary>When the night starts, in local time: the Night filter in History and the sleep card use it (Settings → Sleep).</summary>
    public TimeOnly NightStart { get; init; } = NightWindow.Default.Start;

    /// <summary>When the night ends, in local time. The night may cross midnight; it must not start where it ends.</summary>
    public TimeOnly NightEnd { get; init; } = NightWindow.Default.End;

    public NightWindow Night => new(NightStart, NightEnd);

    public static AppSettings Default { get; } = new(LanguageChoice.System, ThemeChoice.System, true);
}
