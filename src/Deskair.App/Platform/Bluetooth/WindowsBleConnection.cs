using Deskair.Services.Sensors;

namespace Deskair.App.Platform.Bluetooth;

public sealed class WindowsBleConnection : IBleConnection
{
    // TODO(BLE-02a) [L, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Bluetooth"; docs/plans/spikes.md S1.
    // Input : a Bluetooth address string.
    // Output: an open GATT session with the device.
    // Rules : map the address string to the Windows device; dispose the session when this object is disposed.
    // Edge  : connection failure throws; cancellation aborts the attempt.
    // Done  : manual check with a real sensor (spike S1). No automated test.
    public Task ConnectAsync(string address, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(BLE-02a)");

    // TODO(BLE-02b) [M, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Bluetooth".
    // Input : service UUID, characteristic UUID.
    // Output: the characteristic's current value as bytes.
    // Rules : fail clearly when not connected or the characteristic is missing.
    // Edge  : cancellation is honoured before the read.
    // Done  : manual check with a real sensor (spike S1).
    public Task<byte[]> ReadAsync(Guid service, Guid characteristic, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(BLE-02b)");

    // TODO(BLE-02c) [L, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Bluetooth"; docs/plans/spikes.md S1.
    // Input : service UUID, characteristic UUID.
    // Output: each notification value as bytes, until cancelled.
    // Rules : enable notifications on enumeration, disable them on cancellation or dispose.
    // Edge  : a dropped connection ends the sequence with an exception.
    // Done  : manual check with a real sensor (spike S1).
    public IAsyncEnumerable<byte[]> SubscribeAsync(Guid service, Guid characteristic, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(BLE-02c)");

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
