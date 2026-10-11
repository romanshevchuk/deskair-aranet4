using Deskair.Services.Sensors;
using Microsoft.Extensions.DependencyInjection;

namespace Deskair.Sensors.Simulated;

public static class SimulatedRegistration
{
    public static IServiceCollection AddSimulated(this IServiceCollection services)
    {
        services.AddSingleton<ISensorProvider, SimulatedProvider>();

        return services;
    }
}
