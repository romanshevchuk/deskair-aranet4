using Deskair.Services.Sensors;

namespace Deskair.App.Platform.Bluetooth;

public sealed class WindowsBleScanner : IBleScanner
{
    // TODO(BLE-01a) [L, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Bluetooth"; docs/engineering/architecture.md section 10.
    // Input : a CancellationToken.
    // Output: BleAdvertisement for each advertisement the Windows Bluetooth LE watcher reports.
    // Rules : start the watcher on enumeration, stop it on cancellation or dispose; no filtering here (providers filter).
    // Edge  : Bluetooth off ends the sequence with an exception the caller maps to the Not connected state.
    // Done  : needs spike S1 hardware; manual check with the gallery's simulated switch off. No automated test.
    public IAsyncEnumerable<BleAdvertisement> ScanAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(BLE-01a)");
}
