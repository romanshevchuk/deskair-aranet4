using Deskair.Services.Sensors;
using Microsoft.Extensions.DependencyInjection;

namespace Deskair.Sensors.Aranet4;

public static class Aranet4Registration
{
    public static IServiceCollection AddAranet4(this IServiceCollection services)
    {
        services.AddSingleton<Func<IBleConnection>>(provider => () => provider.GetRequiredService<IBleConnection>());
        services.AddSingleton<PhoneExportImporter>();
        services.AddSingleton<ISensorProvider, Aranet4Provider>();

        return services;
    }
}
