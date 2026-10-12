using System.Windows;
using Deskair.App.Localization;
using Deskair.App.Ui.Theme;
using Deskair.App.Ui.Views.Gallery;
using Deskair.App.Ui.Views.Shell;
using Deskair.Services.Live;
using Deskair.Services.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Deskair.App.Composition;

/// <summary>
/// Startup order (docs/engineering/architecture.md section 11): settings, localization and theme first so the window opens in the right
/// language and colours, then the window, then the services that fill it (devices, readings, sensor). The tray comes with its own cards.
/// A sync failure never blocks the window: the services start after it is shown and report problems as connection states.
/// </summary>
public sealed class StartupSequence : IDisposable
{
    private readonly IServiceProvider services;
    private readonly AppOptions options;
    private readonly LanguageService language;
    private readonly ThemeService theme;
    private readonly SettingsService settings;
    private CancellationTokenSource? running;

    public StartupSequence(
        IServiceProvider services,
        AppOptions options,
        LanguageService language,
        ThemeService theme,
        SettingsService settings)
    {
        this.services = services;
        this.options = options;
        this.language = language;
        this.theme = theme;
        this.settings = settings;
    }

    public async Task RunAsync()
    {
        await settings.LoadAsync(CancellationToken.None);
        language.Apply(settings.Current.Language);
        theme.Apply(settings.Current.Theme);

        if (options.Gallery)
        {
            services.GetRequiredService<GalleryWindow>().Show();
            return;
        }

        services.GetRequiredService<MainWindow>().Show();

        running = new CancellationTokenSource();
        await services.GetRequiredService<AppServicesStarter>().StartAsync(running.Token);
    }

    public void Dispose()
    {
        running?.Dispose();
    }

    /// <summary>Stops the background work: the live feed, the sync and the listeners. Called when the app exits.</summary>
    public async Task StopAsync()
    {
        if (running is not null)
        {
            await running.CancelAsync();
        }

        await services.GetRequiredService<SensorSession>().StopAsync();
    }
}
