using Deskair.Services.Platform;
using Microsoft.Win32;

namespace Deskair.App.Platform.Theming;

/// <summary>Reads the Windows "apps use light theme" setting and reports changes.</summary>
public sealed class SystemThemeSource : ISystemTheme, IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string LightThemeValue = "AppsUseLightTheme";

    public SystemThemeSource()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public event EventHandler? Changed;

    public bool IsDark
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);

            return key?.GetValue(LightThemeValue) is 0;
        }
    }

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
