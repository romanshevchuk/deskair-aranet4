using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.Platform;
using Deskair.App.Platform.Bluetooth;
using Deskair.App.Platform.Files;
using Deskair.App.Platform.Launch;
using Deskair.App.Platform.Notifications;
using Deskair.App.Platform.Power;
using Deskair.App.Platform.Startup;
using Deskair.App.Platform.Theming;
using Deskair.App.Platform.Threading;
using Deskair.App.Platform.Tray;
using Deskair.App.Ui.Theme;
using Deskair.App.Ui.Views.Gallery;
using Deskair.App.Ui.Views.Shell;
using Deskair.App.ViewModels.Connection;
using Deskair.App.ViewModels.Data;
using Deskair.App.ViewModels.Flyout;
using Deskair.App.ViewModels.History;
using Deskair.App.ViewModels.Live;
using Deskair.App.ViewModels.Onboarding;
using Deskair.App.ViewModels.Settings;
using Deskair.App.ViewModels.Shell;
using Deskair.App.ViewModels.Tray;
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
using Microsoft.Extensions.Logging;

namespace Deskair.App.Composition;

/// <summary>The one readable place where the app is wired together. Only this folder may use Deskair.Sensors.</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddDeskair(this IServiceCollection services, AppOptions options)
    {
        services.AddSingleton(options);

        services.AddDeskairServices();
        services.AddDeskairStorage();
        // Simulated sensors are for the gallery, tests and --simulated; a real first run must not list one.
        if (options.Simulated || options.Gallery)
        {
            services.AddSimulated();
        }

        services.AddAranet4();

        services.AddWindowsPlatform(options);
        services.AddLocalization();
        services.AddUi();

        return services;
    }

    private static IServiceCollection AddWindowsPlatform(this IServiceCollection services, AppOptions options)
    {
        services.AddSingleton<IDataFolderChoice>(new DataFolderChoice(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Deskair", "data-folder.txt")));
        services.AddSingleton<IKnownFolders, KnownFolders>();
        services.AddSingleton<ILoggerProvider>(provider => new RollingFileLoggerProvider(provider.GetRequiredService<IKnownFolders>().LogFolder, provider.GetRequiredService<TimeProvider>()));
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
        services.AddSingleton<ToastTextBuilder>();
        services.AddSingleton<ToastLogos>();
        services.AddSingleton<ToastActivationServer>();
        services.AddSingleton<ToastActionHandler>();
        services.AddSingleton<INotifier, ToastNotifier>();
        services.AddSingleton<IDoNotDisturbReader, WindowsDoNotDisturbReader>();
        services.AddSingleton<DoNotDisturbMonitor>();
        services.AddSingleton<IDoNotDisturb>(provider => provider.GetRequiredService<DoNotDisturbMonitor>());
        services.AddSingleton<IStartupRegistration, StartupRegistration>();
        services.AddSingleton<IFileDialogs, WindowsFileDialogs>();
        services.AddSingleton<IFolderOpener, ExplorerFolderOpener>();
        services.AddSingleton<PowerEvents>();
        services.AddSingleton<ISystemSettings, WindowsSystemSettings>();

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
        services.AddSingleton<BackdropService>();
        services.AddSingleton<StartupSequence>();
        services.AddSingleton<MetricFormatter>();
        services.AddSingleton<FriendlyTime>();
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
        services.AddSingleton<OnboardingViewModel>();
        services.AddSingleton<OfflineViewModel>();
        services.AddSingleton<TrayViewModel>();
        services.AddSingleton<TrayMenuViewModel>();
        services.AddSingleton<MainWindowController>();
        services.AddSingleton<IMainWindowController>(provider => provider.GetRequiredService<MainWindowController>());
        services.AddSingleton<FlyoutViewModel>();
        services.AddSingleton<FlyoutController>();
        services.AddSingleton<TrayIconHost>();
        services.AddSingleton<AppearanceSettingsViewModel>();
        services.AddSingleton<AlertsSettingsViewModel>();
        services.AddSingleton<SleepSettingsViewModel>();
        services.AddSingleton<DevicesSettingsViewModel>();
        services.AddSingleton<ImportViewModel>();
        services.AddSingleton<ExportViewModel>();
        services.AddSingleton<DataSettingsViewModel>();
        services.AddSingleton<StartupSettingsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<HistoryToolbarViewModel>();
        services.AddSingleton<StatsViewModel>();
        services.AddSingleton<TypicalDayViewModel>();
        services.AddSingleton<SleepViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton(provider => new ShellViewModel(provider.GetRequiredService<IMessenger>())
        {
            Live = provider.GetRequiredService<LiveViewModel>(),
            Connection = provider.GetRequiredService<ConnectionViewModel>(),
            Footer = provider.GetRequiredService<FooterViewModel>(),
            Room = provider.GetRequiredService<RoomChipViewModel>(),
            Onboarding = provider.GetRequiredService<OnboardingViewModel>(),
            Offline = provider.GetRequiredService<OfflineViewModel>(),
            Settings = provider.GetRequiredService<SettingsViewModel>(),
            History = provider.GetRequiredService<HistoryViewModel>()
        });
        services.AddSingleton<SensorSession>();
        services.AddSingleton<ConnectionTracker>();
        services.AddSingleton<AppServicesStarter>();
        services.AddSingleton<SettingsApplier>();
        services.AddTransient<MainWindow>();
        services.AddTransient<GalleryWindow>();

        return services;
    }
}
