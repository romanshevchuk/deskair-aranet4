namespace Deskair.Services.Sensors;

public interface IBleScanner
{
    IAsyncEnumerable<BleAdvertisement> ScanAsync(CancellationToken cancellationToken);
}
