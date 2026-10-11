using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Sensors.Aranet4;

public sealed class Aranet4Sensor : ISensor
{
    private readonly IBleConnection connection;
    private readonly DeviceId id;

    public Aranet4Sensor(IBleConnection connection, DeviceId id)
    {
        this.connection = connection;
        this.id = id;
    }

    public SensorCapabilities Capabilities { get; } = new(History: true, Battery: true, SignalStrength: true);

    // TODO(ARANET-03a) [L, sonnet]
    // Spec: docs/plans/spikes.md S1; docs/engineering/architecture.md section 5.
    // Input : none (uses the connection and the device address).
    // Output: SensorStatus with interval, battery percent and signal.
    // Rules : connect through IBleConnection, then read the interval and battery characteristics.
    // Edge  : connection failure surfaces as an exception the caller turns into a connection state.
    // Done  : unskip Sensors/Aranet4SensorTests.Connect_* with a fake IBleConnection.
    public Task<SensorStatus> ConnectAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(ARANET-03a)");

    // TODO(ARANET-03b) [M, sonnet]
    // Spec: docs/plans/spikes.md S1.
    // Input : none.
    // Output: the current Reading via Aranet4Parser.ParseCurrent.
    // Rules : one ReadAsync call; the timestamp is the read time minus the sensor's reported age.
    // Edge  : cancellation is honoured before the read.
    // Done  : unskip Sensors/Aranet4SensorTests.ReadCurrent_* with a fake IBleConnection.
    public Task<Reading> ReadCurrentAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(ARANET-03b)");

    // TODO(ARANET-03c) [L, sonnet]
    // Spec: docs/plans/spikes.md S1; docs/engineering/sync-and-import.md; AGENTS.md "incomplete history".
    // Input : the instant to read history from.
    // Output: readings streamed in order via Aranet4Parser.ParseHistory.
    // Rules : stream as packets arrive; honour cancellation between packets.
    // Edge  : a dropped connection throws so SyncService reports Complete = false.
    // Done  : unskip Sensors/Aranet4SensorTests.ReadHistory_* with a recorded capture.
    public IAsyncEnumerable<Reading> ReadHistoryAsync(DateTimeOffset since, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(ARANET-03c)");

    public ValueTask DisposeAsync() => connection.DisposeAsync();
}
