using Deskair.Domain.Readings;

namespace Deskair.Services.Sensors;

/// <summary>One per sensor family. Returns resource keys, never text.</summary>
public interface ISensorProvider
{
    string Id { get; }

    string DisplayNameKey { get; }

    IReadOnlyList<string> OnboardingStepKeys { get; }

    IReadOnlyList<IReadingImporter> Importers { get; }

    IAsyncEnumerable<DiscoveredSensor> ScanAsync(CancellationToken cancellationToken);

    ISensor Create(DeviceId id);
}
