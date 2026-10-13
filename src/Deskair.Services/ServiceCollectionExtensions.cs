using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Time;
using Deskair.Services.Alerts;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Live;
using Deskair.Services.Queries;
using Deskair.Services.Settings;
using Deskair.Services.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace Deskair.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeskairServices(this IServiceCollection services)
    {
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(TimeZoneInfo.Local);
        services.AddSingleton(provider => new LocalCalendar(provider.GetRequiredService<TimeZoneInfo>()));

        services.AddSingleton<DeviceRegistry>();
        services.AddSingleton<DeviceEnrollment>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<SyncService>();
        services.AddSingleton<LiveFeed>();
        services.AddSingleton<ConnectionMonitor>();
        services.AddSingleton<AlertEngine>();
        services.AddSingleton<AlertFeed>();
        services.AddSingleton<SnoozeService>();
        services.AddSingleton<ReadingQueryService>();
        services.AddSingleton<ReadingImporters>();
        services.AddSingleton<ImportService>();
        services.AddSingleton<ExportService>();

        return services;
    }
}
