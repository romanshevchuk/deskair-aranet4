using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Services.Import;

/// <summary>Finds the importer for the files a device's own app exports. The first importer of the device's provider is used.</summary>
public sealed class ReadingImporters
{
    private readonly IEnumerable<ISensorProvider> providers;

    public ReadingImporters(IEnumerable<ISensorProvider> providers)
    {
        this.providers = providers;
    }

    /// <summary>The importer for a device, or null when its provider has none (for example the simulated sensor).</summary>
    public IReadingImporter? For(DeviceId device)
    {
        var available = providers.FirstOrDefault(provider => provider.Id == device.ProviderId)?.Importers;

        return available is { Count: > 0 } ? available[0] : null;
    }
}
