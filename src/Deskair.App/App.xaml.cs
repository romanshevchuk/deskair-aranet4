using System.Windows;
using Deskair.App.Composition;
using Deskair.App.Localization;
using Deskair.App.Ui.Markup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deskair.App;

public partial class App : Application
{
    private readonly IServiceProvider services;

    public App(IServiceProvider services)
    {
        this.services = services;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The {loc:T} markup extension finds the localizer here; it must exist before any window is created.
        Resources[TExtension.LocalizerResourceKey] = services.GetRequiredService<Localizer>();

        try
        {
            await services.GetRequiredService<StartupSequence>().RunAsync();
        }
        catch (Exception exception)
        {
            var logger = services.GetRequiredService<ILogger<App>>();

            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.LogCritical(exception, "Deskair could not start.");
            }

            var text = services.GetRequiredService<ILocalizer>().Format("Startup_Failed", ("message", exception.Message));

            MessageBox.Show(text, "Deskair", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Stop the sensor loops without blocking the UI thread on work that may need it.
        Task.Run(() => services.GetRequiredService<StartupSequence>().StopAsync()).Wait(TimeSpan.FromSeconds(3));

        base.OnExit(e);
    }
}
