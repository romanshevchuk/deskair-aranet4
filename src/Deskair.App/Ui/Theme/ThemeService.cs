using System.Windows;
using Deskair.Services.Platform;
using Deskair.Services.Settings;

namespace Deskair.App.Ui.Theme;

/// <summary>
/// Swaps the light or dark token dictionaries in the application resources. Controls use
/// DynamicResource, so every window follows without being rebuilt.
/// </summary>
public sealed class ThemeService
{
    private static readonly string[] themedDictionaries = ["Colors", "Zones", "Metrics"];

    private readonly ISystemTheme systemTheme;
    private readonly List<ResourceDictionary> active = new();
    private ThemeChoice choice = ThemeChoice.System;

    public ThemeService(ISystemTheme systemTheme)
    {
        this.systemTheme = systemTheme;
        this.systemTheme.Changed += OnSystemThemeChanged;
    }

    public event EventHandler? ThemeChanged;

    public bool IsDark { get; private set; }

    public void Apply(ThemeChoice newChoice)
    {
        choice = newChoice;
        IsDark = newChoice switch
        {
            ThemeChoice.Dark => true,
            ThemeChoice.Light => false,
            _ => systemTheme.IsDark
        };

        ReplaceDictionaries();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ReplaceDictionaries()
    {
        var merged = Application.Current.Resources.MergedDictionaries;

        foreach (var old in active)
        {
            merged.Remove(old);
        }

        active.Clear();

        var suffix = IsDark ? "Dark" : "Light";

        foreach (var name in themedDictionaries)
        {
            var uri = new Uri($"/Deskair.App;component/Ui/Theme/Tokens/{name}.{suffix}.xaml", UriKind.Relative);
            var dictionary = new ResourceDictionary { Source = uri };

            merged.Add(dictionary);
            active.Add(dictionary);
        }
    }

    private void OnSystemThemeChanged(object? sender, EventArgs e)
    {
        if (choice != ThemeChoice.System)
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() => Apply(ThemeChoice.System));
    }
}
