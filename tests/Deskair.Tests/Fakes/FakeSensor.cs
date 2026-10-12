using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Tests.Fakes;

/// <summary>A sensor that returns what a test gives it, and can fail or pause part way through a history download.</summary>
public sealed class FakeSensor : ISensor
{
    private int running;

    public SensorCapabilities Capabilities { get; set; } = new(History: true, Battery: true, SignalStrength: true);

    public List<Reading> History { get; } = [];

    /// <summary>Throw after this many readings of a history download.</summary>
    public int? FailHistoryAfter { get; set; }

    /// <summary>Wait for cancellation after this many readings of a history download.</summary>
    public int? PauseHistoryAfter { get; set; }

    public TaskCompletionSource HistoryPaused { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public DateTimeOffset? HistorySince { get; private set; }

    public int HistoryCalls { get; private set; }

    /// <summary>Given the number of earlier calls (0 for the first), returns the reading or throws.</summary>
    public Func<int, Reading> OnReadCurrent { get; set; }
        = call => Samples.Co2(Samples.Utc(2026, 10, 9, 12).AddMinutes(2 * call), 800 + call);

    public int ReadCurrentCalls { get; private set; }

    public int MaxConcurrentReads { get; private set; }

    public Task<SensorStatus> ConnectAsync(CancellationToken cancellationToken)
        => Task.FromResult(new SensorStatus(TimeSpan.FromMinutes(2), 80, -60));

    public async Task<Reading> ReadCurrentAsync(CancellationToken cancellationToken)
    {
        var call = ReadCurrentCalls++;
        var now = Interlocked.Increment(ref running);
        MaxConcurrentReads = Math.Max(MaxConcurrentReads, now);

        try
        {
            await Task.Yield();

            return OnReadCurrent(call);
        }
        finally
        {
            Interlocked.Decrement(ref running);
        }
    }

    public async IAsyncEnumerable<Reading> ReadHistoryAsync(
        DateTimeOffset since,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        HistoryCalls++;
        HistorySince = since;

        for (var index = 0; index < History.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (index == FailHistoryAfter)
            {
                throw new InvalidOperationException("The sensor dropped the connection.");
            }

            if (index == PauseHistoryAfter)
            {
                HistoryPaused.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            await Task.Yield();

            yield return History[index];
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
