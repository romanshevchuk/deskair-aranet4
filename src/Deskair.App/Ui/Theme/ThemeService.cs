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
    private readonly Func<HighContrastColors?> highContrast;
    private readonly List<ResourceDictionary> active = new();
    private ThemeChoice choice = ThemeChoice.System;

    /// <param name="systemTheme">Tells whether Windows is dark.</param>
    /// <param name="highContrast">The High Contrast colours while Windows High Contrast is on, otherwise null. Defaults to the system's own.</param>
    public ThemeService(ISystemTheme systemTheme, Func<HighContrastColors?>? highContrast = null)
    {
        this.systemTheme = systemTheme;
        this.highContrast = highContrast ?? SystemHighContrast;
        this.systemTheme.Changed += OnSystemThemeChanged;
        SystemParameters.StaticPropertyChanged += OnSystemParameterChanged;
    }

    public event EventHandler? ThemeChanged;

    public bool IsDark { get; private set; }

    /// <summary>True while the tokens are the Windows High Contrast colours.</summary>
    public bool IsHighContrast { get; private set; }

    private static HighContrastColors? SystemHighContrast() => SystemParameters.HighContrast ? HighContrastColors.FromSystem() : null;

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

        var colors = highContrast();

        IsHighContrast = colors is not null;

        if (colors is { } contrast)
        {
            var overlay = HighContrastPalette.Create(active.ToList(), contrast);

            merged.Add(overlay);
            active.Add(overlay);
        }
    }

    private void OnSystemParameterChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.HighContrast) && Application.Current is { } application)
        {
            application.Dispatcher.Invoke(() => Apply(choice));
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
