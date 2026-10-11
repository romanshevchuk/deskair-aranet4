using System.Windows;
using Deskair.App.Composition;
using Deskair.App.Localization;
using Deskair.App.Ui.Markup;
using Microsoft.Extensions.DependencyInjection;

namespace Deskair.App;

public partial class App : Application
{
    private readonly IServiceProvider services;

    public App(IServiceProvider services)
    {
        this.services = services;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The {loc:T} markup extension finds the localizer here; it must exist before any window is created.
        Resources[TExtension.LocalizerResourceKey] = services.GetRequiredService<Localizer>();

        services.GetRequiredService<StartupSequence>().Run();
    }
}
