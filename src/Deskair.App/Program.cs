using Deskair.App.Composition;
using Deskair.App.Composition.Spikes;
using Deskair.App.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Deskair.App;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var options = AppOptions.Parse(args);

        if (options.FirstRun)
        {
            KnownFolders.ResetFirstRunFolder();
        }

        if (options.SpikeS1)
        {
            return SpikeS1.Run(options);
        }

        // The gallery is a development tool, so it may run next to the real app.
        using var instance = new SingleInstance(options.Gallery ? "Deskair.Gallery" : "Deskair");

        if (!instance.IsFirst)
        {
            // Another Deskair is running: ask it to show its window (it may be hidden in the tray) and leave.
            instance.SignalFirstInstance();
            return 0;
        }

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddDeskair(options);
        builder.Services.AddSingleton(instance);

        using var host = builder.Build();

        var app = new App(host.Services);
        app.InitializeComponent();

        return app.Run();
    }
}
