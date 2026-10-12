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

    public static AppSettings Default { get; } = new(LanguageChoice.System, ThemeChoice.System, true);
}
