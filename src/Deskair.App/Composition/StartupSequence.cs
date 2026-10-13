using System.IO;
using System.Security;
using System.Windows;
using Deskair.App.Localization;
using Deskair.App.Platform.Notifications;
using Deskair.App.Platform.Power;
using Deskair.App.Platform.Tray;
using Deskair.App.Ui.Theme;
using Deskair.App.Ui.Views.Gallery;
using Deskair.App.Ui.Views.Shell;
using Deskair.Services.Alerts;
using Deskair.Services.Live;
using Deskair.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Composition;

/// <summary>
/// Startup order (docs/engineering/architecture.md section 11): settings, localization and theme first so the window opens in the right
/// language and colours, then the window, then the services that fill it (devices, readings, sensor). The tray icon starts right after the window.
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
        services.GetRequiredService<SettingsApplier>().Start(CancellationToken.None);

        if (options.Gallery)
        {
            services.GetRequiredService<GalleryWindow>().Show();
            return;
        }

        StartToasts();

        var window = services.GetRequiredService<MainWindow>();
        var trayStarted = StartTray(window);

        // Windows starts the app in the background when a toast button is pressed while it is not running; without a tray icon there would be no way to reach it.
        if (!options.StartedByToast || !trayStarted)
        {
            window.Show();
        }

        ShowWindowOnSecondLaunch(window);
        services.GetRequiredService<PowerEvents>().Start();

        running = new CancellationTokenSource();
        services.GetRequiredService<AlertFeed>().Start(running.Token);
        await services.GetRequiredService<AppServicesStarter>().StartAsync(running.Token);
    }

    /// <summary>
    /// Puts the icon in the notification area. Only when that worked does closing the main window hide it instead of closing it, so a failing tray
    /// never leaves the user with a running app and no way to reach it.
    /// </summary>
    private bool StartTray(MainWindow window)
    {
        var controller = services.GetRequiredService<MainWindowController>();
        controller.Attach(window);

        if (!services.GetRequiredService<TrayIconHost>().Start())
        {
            return false;
        }

        controller.KeepInTray();

        return true;
    }

    /// <summary>
    /// Gets the app ready for toasts: the identity Windows needs, and the listener for button presses. This runs before the window so a press that started the app
    /// finds the listener at once. A failure is logged; toasts then may not show, and the app works without them.
    /// </summary>
    private void StartToasts()
    {
        var logger = services.GetRequiredService<ILogger<StartupSequence>>();

        try
        {
            ToastIdentity.Register(Environment.ProcessPath ?? string.Empty);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or SecurityException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Registering the toast identity failed.");
            }
        }

        var server = services.GetRequiredService<ToastActivationServer>();

        if (server.Start())
        {
            server.Attach(services.GetRequiredService<ToastActionHandler>().Handle);
        }
    }

    /// <summary>A second launch of the app brings this window back, also when it is hidden in the tray or minimized.</summary>
    private void ShowWindowOnSecondLaunch(MainWindow window)
    {
        var controller = services.GetRequiredService<MainWindowController>();

        services.GetService<SingleInstance>()?.Listen(() => window.Dispatcher.BeginInvoke(() => controller.Show()));
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
