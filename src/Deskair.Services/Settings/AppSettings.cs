namespace Deskair.Services.Settings;

/// <summary>User preferences. The full list is still open (see docs/plans/questions.md Q6).</summary>
public sealed record AppSettings(
    LanguageChoice Language,
    ThemeChoice Theme,
    bool AlertsEnabled)
{
    public static AppSettings Default { get; } = new(LanguageChoice.System, ThemeChoice.System, true);
}
