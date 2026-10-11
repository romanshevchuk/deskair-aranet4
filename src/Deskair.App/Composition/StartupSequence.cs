using System.Windows;
using Deskair.App.Localization;
using Deskair.App.Ui.Theme;
using Deskair.App.Ui.Views.Gallery;
using Deskair.App.Ui.Views.Shell;
using Deskair.Services.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Deskair.App.Composition;

/// <summary>
/// Startup order (docs/engineering/architecture.md section 11). The skeleton runs the parts that exist:
/// localization, theme and the first window, using default settings.
/// </summary>
public sealed class StartupSequence
{
    private readonly IServiceProvider services;
    private readonly AppOptions options;
    private readonly LanguageService language;
    private readonly ThemeService theme;

    public StartupSequence(
        IServiceProvider services,
        AppOptions options,
        LanguageService language,
        ThemeService theme)
    {
        this.services = services;
        this.options = options;
        this.language = language;
        this.theme = theme;
    }

    public void Run()
    {
        var settings = AppSettings.Default;

        language.Apply(settings.Language);
        theme.Apply(settings.Theme);

        Window window = options.Gallery
            ? services.GetRequiredService<GalleryWindow>()
            : services.GetRequiredService<MainWindow>();

        window.Show();
    }

    // TODO(STARTUP-01a) [L, sonnet]
    // Spec: docs/engineering/architecture.md section 11 (startup order); docs/product/behaviour-rules.md "Sync and data".
    // Input : a CancellationToken.
    // Output: settings loaded, storage opened and indexed, tray started, LiveFeed and SyncService running for the selected device.
    // Rules : follow the documented order; the window is shown unless "start in tray"; cancellation stops sensor loops.
    // Edge  : first run has no device (show onboarding); a sync failure must not block the window.
    // Done  : manual check with the simulated sensor; add a test for the order with fake services if practical.
    public Task StartServicesAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(STARTUP-01a)");
}
