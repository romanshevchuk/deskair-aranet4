namespace Deskair.Services.Sensors;

public interface IBleScanner
{
    IAsyncEnumerable<BleAdvertisement> ScanAsync(CancellationToken cancellationToken);

    /// <summary>Scans in the given mode. A scanner that has only one mode keeps using it.</summary>
    IAsyncEnumerable<BleAdvertisement> ScanAsync(BleScanMode mode, CancellationToken cancellationToken)
        => ScanAsync(cancellationToken);
}
