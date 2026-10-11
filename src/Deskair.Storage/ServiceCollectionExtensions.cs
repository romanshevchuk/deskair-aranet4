using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Settings;
using Deskair.Services.Storage;
using Deskair.Storage.Devices;
using Deskair.Storage.Export;
using Deskair.Storage.Readings;
using Deskair.Storage.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Deskair.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeskairStorage(this IServiceCollection services)
    {
        services.AddSingleton<IReadingStore, MonthlyCsvStore>();
        services.AddSingleton<IDeviceStore, DeviceFileStore>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<IReadingExporter, CsvExporter>();

        return services;
    }
}
