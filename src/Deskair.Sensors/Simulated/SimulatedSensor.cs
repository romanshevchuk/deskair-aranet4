using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Sensors.Simulated;

/// <summary>Deterministic readings: the same instant always gives the same reading, so tests can rely on them.</summary>
public sealed class SimulatedSensor : ISensor
{
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan interval;

    public SimulatedSensor(TimeProvider timeProvider, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "The interval must be positive.");
        }

        this.timeProvider = timeProvider;
        this.interval = interval;
    }

    public SensorCapabilities Capabilities { get; } = new(History: true, Battery: true, SignalStrength: true);

    public Task<SensorStatus> ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new SensorStatus(interval, 87, -55));
    }

    public Task<Reading> ReadCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var slot = SnapDown(timeProvider.GetUtcNow());

        return Task.FromResult(ReadingAt(slot));
    }

    public async IAsyncEnumerable<Reading> ReadHistoryAsync(
        DateTimeOffset since,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var end = SnapDown(timeProvider.GetUtcNow());
        var slot = SnapDown(since);

        if (slot < since)
        {
            slot += interval;
        }

        while (slot <= end)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return ReadingAt(slot);
            slot += interval;
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>The reading the simulated room would show at this instant.</summary>
    public static Reading ReadingAt(DateTimeOffset timestamp)
    {
        var day = timestamp.UtcDateTime.TimeOfDay.TotalHours / 24.0;
        var ripple = Math.Sin(timestamp.ToUnixTimeSeconds() / 900.0);

        // One slow wave a day: about 450 ppm at night, about 1,500 ppm late afternoon.
        var occupancy = (Math.Sin((day - 0.3) * 2 * Math.PI) + 1) / 2;
        var co2 = 450 + (1050 * occupancy) + (30 * ripple);

        var temperature = 21.5 + (2.0 * occupancy) + (0.2 * ripple);
        var humidity = 42 - (6 * occupancy) + (1.0 * ripple);
        var pressure = 1013 + (4 * Math.Sin(timestamp.ToUnixTimeSeconds() / 86400.0));

        return new Reading(
            timestamp,
            Math.Round(co2),
            Math.Round(temperature, 1),
            Math.Round(humidity),
            Math.Round(pressure, 1));
    }

    private DateTimeOffset SnapDown(DateTimeOffset instant)
    {
        var ticks = instant.UtcTicks - (instant.UtcTicks % interval.Ticks);

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
