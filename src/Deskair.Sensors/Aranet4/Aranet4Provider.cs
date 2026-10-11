using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Sensors.Aranet4;

public sealed class Aranet4Provider : ISensorProvider
{
    public const string ProviderId = "aranet4";

    private readonly IBleScanner scanner;
    private readonly Func<IBleConnection> connectionFactory;

    public Aranet4Provider(
        IBleScanner scanner,
        Func<IBleConnection> connectionFactory,
        PhoneExportImporter phoneExport)
    {
        this.scanner = scanner;
        this.connectionFactory = connectionFactory;
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

    // TODO(ARANET-04a) [M, sonnet]
    // Spec: docs/plans/spikes.md S1; docs/engineering/architecture.md section 5.
    // Input : a CancellationToken.
    // Output: DiscoveredSensor for each advertisement that looks like this sensor.
    // Rules : filter IBleScanner advertisements with a pure check on name or manufacturer data (from the spike).
    // Edge  : the same address is reported once.
    // Done  : unskip Sensors/Aranet4ProviderTests.Scan_* with a fake IBleScanner.
    public IAsyncEnumerable<DiscoveredSensor> ScanAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(ARANET-04a)");

    public ISensor Create(DeviceId id) => new Aranet4Sensor(connectionFactory(), id);
}
