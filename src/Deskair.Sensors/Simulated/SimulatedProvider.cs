using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Sensors.Simulated;

/// <summary>A sensor that needs no hardware. It feeds the gallery and the tests.</summary>
public sealed class SimulatedProvider : ISensorProvider
{
    public const string ProviderId = "simulated";

    private static readonly TimeSpan defaultInterval = TimeSpan.FromMinutes(2);

    private readonly TimeProvider timeProvider;

    public SimulatedProvider(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public string Id => ProviderId;

    public string DisplayNameKey => "Sensor_Simulated_Name";

    public IReadOnlyList<string> OnboardingStepKeys { get; } = ["Sensor_Simulated_Step1"];

    public IReadOnlyList<IReadingImporter> Importers { get; } = [];

    public async IAsyncEnumerable<DiscoveredSensor> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        yield return new DiscoveredSensor(new DeviceId(ProviderId, "SIM-0001"), "Simulated sensor", -55);
    }

    public ISensor Create(DeviceId id) => new SimulatedSensor(timeProvider, defaultInterval);
}
