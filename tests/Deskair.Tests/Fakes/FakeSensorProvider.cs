using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Tests.Fakes;

/// <summary>A sensor provider that hands out a <see cref="FakeSensor"/> and remembers which devices it was asked for.</summary>
public sealed class FakeSensorProvider : ISensorProvider
{
    private readonly FakeSensor sensor;

    public FakeSensorProvider(string id, FakeSensor sensor, params IReadingImporter[] importers)
    {
        Id = id;
        this.sensor = sensor;
        Importers = importers;
    }

    public List<DeviceId> Created { get; } = [];

    public string Id { get; }

    public string DisplayNameKey => "Sensor_Simulated_Name";

    public IReadOnlyList<string> OnboardingStepKeys { get; } = [];

    public IReadOnlyList<IReadingImporter> Importers { get; }

    public async IAsyncEnumerable<DiscoveredSensor> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break;
    }

    public ISensor Create(DeviceId id)
    {
        Created.Add(id);

        return sensor;
    }
}
