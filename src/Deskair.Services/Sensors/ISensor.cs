using Deskair.Domain.Readings;

namespace Deskair.Services.Sensors;

public interface ISensor : IAsyncDisposable
{
    SensorCapabilities Capabilities { get; }

    Task<SensorStatus> ConnectAsync(CancellationToken cancellationToken);

    Task<Reading> ReadCurrentAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<Reading> ReadHistoryAsync(DateTimeOffset since, CancellationToken cancellationToken);
}
