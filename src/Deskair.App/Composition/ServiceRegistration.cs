using System.Globalization;
using Deskair.App.Localization;
using Deskair.App.Platform;
using Deskair.App.Platform.Bluetooth;
using Deskair.App.Platform.Notifications;
using Deskair.App.Platform.Startup;
using Deskair.App.Platform.Theming;
using Deskair.App.Platform.Threading;
using Deskair.App.Ui.Theme;
using Deskair.App.Ui.Views.Gallery;
using Deskair.App.Ui.Views.Shell;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Live;
using Deskair.App.ViewModels.Shell;
using Deskair.Sensors.Aranet4;
using Deskair.Sensors.Simulated;
using Deskair.Services;
using Deskair.Services.Alerts;
using Deskair.Services.Connection;
using Deskair.Services.Live;
using Deskair.Services.Platform;
using Deskair.Services.Sensors;
using Deskair.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Deskair.App.Composition;

/// <summary>The one readable place where the app is wired together. Only this folder may use Deskair.Sensors.</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddDeskair(this IServiceCollection services, AppOptions options)
    {
        services.AddSingleton(options);

        services.AddDeskairServices();
        services.AddDeskairStorage();
        services.AddSimulated();
        services.AddAranet4();

        services.AddWindowsPlatform(options);
        services.AddLocalization();
        services.AddUi();

        return services;
    }

    private static IServiceCollection AddWindowsPlatform(this IServiceCollection services, AppOptions options)
    {
        services.AddSingleton<IKnownFolders, KnownFolders>();
        services.AddSingleton<ISystemTheme, SystemThemeSource>();
        services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
        // The simulated app needs no Bluetooth hardware.
        if (options.Simulated)
        {
            services.AddSingleton<IBluetoothRadio, SimulatedRadio>();
        }
        else
        {
            services.AddSingleton<IBluetoothRadio, BluetoothRadioMonitor>();
        }

        services.AddSingleton<IBleScanner, WindowsBleScanner>();
        services.AddTransient<IBleConnection, WindowsBleConnection>();
        services.AddSingleton<INotifier, ToastNotifier>();
        services.AddSingleton<IDoNotDisturb, DoNotDisturbMonitor>();
        services.AddSingleton<IStartupRegistration, StartupRegistration>();

        return services;
    }

    private static IServiceCollection AddLocalization(this IServiceCollection services)
    {
        services.AddSingleton<Localizer>();
        services.AddSingleton<ILocalizer>(provider => provider.GetRequiredService<Localizer>());
        services.AddSingleton(provider => new LanguageService(
            provider.GetRequiredService<Localizer>(),
            provider.GetRequiredService<CommunityToolkit.Mvvm.Messaging.IMessenger>(),
            CultureInfo.InstalledUICulture));

        return services;
    }

    private static IServiceCollection AddUi(this IServiceCollection services)
    {
        services.AddSingleton<ThemeService>();
        services.AddSingleton<StartupSequence>();
        services.AddSingleton<MetricFormatter>();
        services.AddSingleton<HeroStatsBuilder>();
        services.AddSingleton<HeroViewModel>();
        services.AddSingleton<GaugeViewModel>();
        services.AddSingleton<MetricTabsViewModel>();
        services.AddSingleton<ChartCardViewModel>();
        services.AddSingleton<MiniCardsViewModel>();
        services.AddSingleton<LiveViewModel>();
        services.AddSingleton<ConnectionViewModel>();
        services.AddSingleton<FooterViewModel>();
        services.AddSingleton<RoomChipViewModel>();
        services.AddSingleton(provider => new ShellViewModel
        {
            Live = provider.GetRequiredService<LiveViewModel>(),
            Connection = provider.GetRequiredService<ConnectionViewModel>(),
            Footer = provider.GetRequiredService<FooterViewModel>(),
            Room = provider.GetRequiredService<RoomChipViewModel>()
        });
        services.AddSingleton<SensorSession>();
        services.AddSingleton<ConnectionTracker>();
        services.AddSingleton<AppServicesStarter>();
        services.AddTransient<MainWindow>();
        services.AddTransient<GalleryWindow>();

        return services;
    }
}
