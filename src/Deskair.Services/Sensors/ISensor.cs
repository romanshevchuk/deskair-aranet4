using Deskair.Domain.Readings;

namespace Deskair.Services.Sensors;

public interface ISensor : IAsyncDisposable
{
    SensorCapabilities Capabilities { get; }

    Task<SensorStatus> ConnectAsync(CancellationToken cancellationToken);

    Task<Reading> ReadCurrentAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<Reading> ReadHistoryAsync(DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>
    /// Like <see cref="ReadHistoryAsync(DateTimeOffset, CancellationToken)"/>, and reports how far a long download is.
    /// A sensor that cannot say how far it is simply does not report.
    /// </summary>
    IAsyncEnumerable<Reading> ReadHistoryAsync(DateTimeOffset since, IProgress<HistoryProgress>? progress, CancellationToken cancellationToken)
        => ReadHistoryAsync(since, cancellationToken);
}
