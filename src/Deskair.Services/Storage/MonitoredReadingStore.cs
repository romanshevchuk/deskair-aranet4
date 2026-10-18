using System.IO;
using Deskair.Domain.Readings;
using Microsoft.Extensions.Logging;

namespace Deskair.Services.Storage;

/// <summary>
/// Wraps the reading store so that a file that cannot be read (locked, no access, folder gone) shows as "nothing stored" plus a report to
/// <see cref="HistoryReadMonitor"/>, instead of an exception that stops Live or History. Writing is not wrapped: a failed merge still reaches the sync.
/// </summary>
public sealed class MonitoredReadingStore : IReadingStore
{
    private readonly IReadingStore inner;
    private readonly HistoryReadMonitor monitor;
    private readonly ILogger<MonitoredReadingStore> logger;

    public MonitoredReadingStore(IReadingStore inner, HistoryReadMonitor monitor, ILogger<MonitoredReadingStore> logger)
    {
        this.inner = inner;
        this.monitor = monitor;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<Reading>> ReadRangeAsync(DeviceId device, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        return await ReadAsync(() => inner.ReadRangeAsync(device, start, end, cancellationToken), []);
    }

    public async Task<DateTimeOffset?> GetFirstReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
    {
        return await ReadAsync(() => inner.GetFirstReadingTimeAsync(device, cancellationToken), null);
    }

    public async Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
    {
        return await ReadAsync(() => inner.GetLastReadingTimeAsync(device, cancellationToken), null);
    }

    public Task<int> MergeAsync(DeviceId device, IReadOnlyList<Reading> readings, ReadingSource source, TimeSpan interval, CancellationToken cancellationToken)
    {
        return inner.MergeAsync(device, readings, source, interval, cancellationToken);
    }

    private async Task<T> ReadAsync<T>(Func<Task<T>> read, T whenUnreadable)
    {
        try
        {
            var result = await read();
            monitor.ReportSuccess();

            return result;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "The saved readings could not be read.");
            }

            monitor.ReportFailure();

            return whenUnreadable;
        }
    }
}
