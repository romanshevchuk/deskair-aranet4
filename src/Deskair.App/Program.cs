using Deskair.App.Composition;
using Deskair.App.Platform;
using Deskair.App.Platform.Packaging;
using Deskair.App.Platform.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deskair.App;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var options = AppOptions.Parse(args);

        if (!options.StartedInBackground && new WindowsPackageIdentity().IsPackaged && PackagedStartupActivation.WasStartedByStartupTask())
        {
            options = options with { StartedInBackground = true };
        }

        if (options.FirstRun)
        {
            KnownFolders.ResetFirstRunFolder();
        }

        // The gallery is a development tool, so it may run next to the real app.
        using var instance = new SingleInstance(options.Gallery ? "Deskair.Gallery" : "Deskair");

        if (!instance.IsFirst)
        {
            // Another Deskair is running: ask it to show its window (it may be hidden in the tray) and leave.
            instance.SignalFirstInstance();
            return 0;
        }

        // Only a service container is needed, so the generic host (configuration files, extra log sinks) is left out.
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Information));
        services.AddDeskair(options);
        services.AddSingleton(instance);

        using var provider = services.BuildServiceProvider();

        var app = new App(provider);
        app.InitializeComponent();

        return app.Run();
    }
}
