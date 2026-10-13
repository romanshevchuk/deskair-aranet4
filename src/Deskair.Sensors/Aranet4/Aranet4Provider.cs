using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Sensors.Aranet4;

public sealed class Aranet4Provider : ISensorProvider
{
    public const string ProviderId = "aranet4";

    private readonly IBleScanner scanner;
    private readonly Func<IBleConnection> connectionFactory;
    private readonly TimeProvider timeProvider;

    public Aranet4Provider(
        IBleScanner scanner,
        Func<IBleConnection> connectionFactory,
        PhoneExportImporter phoneExport,
        TimeProvider timeProvider)
    {
        this.scanner = scanner;
        this.connectionFactory = connectionFactory;
        this.timeProvider = timeProvider;
        Importers = [phoneExport];
    }

    public string Id => ProviderId;

    public string DisplayNameKey => "Sensor_Aranet4_Name";

    public IReadOnlyList<string> OnboardingStepKeys { get; } =
    [
        "Sensor_Aranet4_Step1",
        "Sensor_Aranet4_Step2"
    ];

    public IReadOnlyList<IReadingImporter> Importers { get; }

    /// <summary>True when the advertisement names an Aranet4 or carries the manufacturer's data block (both seen in the spike scan).</summary>
    public static bool IsAranet4(BleAdvertisement advertisement)
        => advertisement.LocalName?.StartsWith(Aranet4Protocol.NamePrefix, StringComparison.OrdinalIgnoreCase) == true
            || advertisement.ManufacturerData.ContainsKey(Aranet4Protocol.ManufacturerId);

    /// <summary>
    /// Reports each Aranet4 address once, with the name and signal of the first advertisement seen. A sensor sends its name in a separate packet,
    /// so an address first seen without a name is reported once more when the name arrives.
    /// </summary>
    public async IAsyncEnumerable<DiscoveredSensor> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var seen = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        await foreach (var advertisement in scanner.ScanAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            var named = !string.IsNullOrEmpty(advertisement.LocalName);

            if (IsAranet4(advertisement) && (!seen.TryGetValue(advertisement.Address, out var known) || (named && !known)))
            {
                seen[advertisement.Address] = named;
                yield return new DiscoveredSensor(new DeviceId(ProviderId, advertisement.Address), advertisement.LocalName, advertisement.SignalDbm);
            }
        }
    }

    public ISensor Create(DeviceId id) => new Aranet4Sensor(connectionFactory(), id, timeProvider);
}
