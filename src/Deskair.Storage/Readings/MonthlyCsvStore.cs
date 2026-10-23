using System.Globalization;
using System.Text;
using Deskair.Domain.Readings;
using Deskair.Services.Platform;
using Deskair.Services.Storage;
using Deskair.Services.Sync;
using Deskair.Storage.Devices;
using Microsoft.Extensions.Logging;

namespace Deskair.Storage.Readings;

/// <summary>
/// Stores readings in <c>devices/&lt;id&gt;/readings-YYYY-MM.csv</c> under the data folder.
/// History is append-only: rows are only ever added to a month file (docs/decisions.md D-0013).
/// </summary>
public sealed class MonthlyCsvStore : IReadingStore, IDisposable
{
    private const char NewLine = '\n';

    private readonly IKnownFolders folders;
    private readonly Dictionary<DeviceId, ReadingIndex> indexes = new();
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ILogger<MonthlyCsvStore> logger;
    private readonly TimeProvider timeProvider;

    public MonthlyCsvStore(IKnownFolders folders, ILogger<MonthlyCsvStore> logger, TimeProvider timeProvider)
    {
        this.folders = folders;
        this.logger = logger;
        this.timeProvider = timeProvider;
    }

    public void Dispose()
    {
        gate.Dispose();
    }

    public async Task<IReadOnlyList<Reading>> ReadRangeAsync(
        DeviceId device,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);

        try
        {
            var index = await LoadAsync(device, cancellationToken);

            return index.Range(start, end);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DateTimeOffset?> GetFirstReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);

        try
        {
            var index = await LoadAsync(device, cancellationToken);

            return index.First();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DateTimeOffset?> GetLastReadingTimeAsync(DeviceId device, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);

        try
        {
            var index = await LoadAsync(device, cancellationToken);

            return index.Last();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<int> MergeAsync(
        DeviceId device,
        IReadOnlyList<Reading> readings,
        ReadingSource source,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        if (readings.Count == 0)
        {
            return 0;
        }

        await gate.WaitAsync(cancellationToken);

        try
        {
            var index = await LoadAsync(device, cancellationToken);
            var from = readings.Min(reading => reading.Timestamp) - interval;
            var to = readings.Max(reading => reading.Timestamp) + interval + TimeSpan.FromTicks(1);
            var added = SlotMerger.Merge(index.Range(from, to), readings, interval);

            if (added.Count == 0)
            {
                return 0;
            }

            var folder = DeviceFolders.For(folders, device);
            Directory.CreateDirectory(folder);

            foreach (var month in added.GroupBy(reading => reading.Timestamp.UtcDateTime.ToString("yyyy-MM", CultureInfo.InvariantCulture)))
            {
                var path = Path.Combine(folder, $"readings-{month.Key}.csv");
                await AppendAsync(path, month.Select(reading => CsvReadingFormat.Write(reading, source)), cancellationToken);
            }

            index.Add(added);

            return added.Count;
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task AppendAsync(string path, IEnumerable<string> rows, CancellationToken cancellationToken)
    {
        var text = new StringBuilder();

        if (!File.Exists(path))
        {
            text.Append(CsvReadingFormat.Header).Append(NewLine);
        }
        else if (!EndsWithNewLine(path))
        {
            // A half-written last line stays on its own line, where loading ignores it.
            text.Append(NewLine);
        }

        foreach (var row in rows)
        {
            text.Append(row).Append(NewLine);
        }

        await using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        await stream.WriteAsync(new UTF8Encoding(false).GetBytes(text.ToString()), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static bool EndsWithNewLine(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        if (stream.Length == 0)
        {
            return true;
        }

        stream.Seek(-1, SeekOrigin.End);

        return stream.ReadByte() == '\n';
    }

    private async Task<ReadingIndex> LoadAsync(DeviceId device, CancellationToken cancellationToken)
    {
        if (indexes.TryGetValue(device, out var cached))
        {
            return cached;
        }

        var started = timeProvider.GetTimestamp();
        var folder = DeviceFolders.For(folders, device);

        // Years of rows are split and parsed here, so this must not run on the caller's (UI) thread. Nothing is cached when it is cancelled.
        var (index, count) = await Task.Run(() => BuildIndex(folder, cancellationToken), cancellationToken);

        indexes[device] = index;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Loaded {Count} readings for {Address} in {Milliseconds} ms.", count, device.Address, (long)timeProvider.GetElapsedTime(started).TotalMilliseconds);
        }

        return index;
    }

    private static (ReadingIndex Index, int Count) BuildIndex(string folder, CancellationToken cancellationToken)
    {
        var loaded = ReadAll(folder, cancellationToken);
        var index = new ReadingIndex();
        index.Add(loaded);

        return (index, loaded.Count);
    }

    private static List<Reading> ReadAll(string folder, CancellationToken cancellationToken)
    {
        var loaded = new List<Reading>();

        if (!Directory.Exists(folder))
        {
            return loaded;
        }

        foreach (var file in Directory.GetFiles(folder, "readings-*.csv").Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var line in File.ReadLines(file))
            {
                if (CsvReadingFormat.TryParse(line, out var reading, out _))
                {
                    loaded.Add(reading);
                }
            }
        }

        return loaded;
    }
}
