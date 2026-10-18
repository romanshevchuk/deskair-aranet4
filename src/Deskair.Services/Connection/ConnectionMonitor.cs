namespace Deskair.Services.Connection;

/// <summary>Derives the connection state from last reading time, radio state, battery and whether a connection attempt is still running.</summary>
public sealed class ConnectionMonitor
{
    private static readonly TimeSpan minimumNotConnectedAfter = TimeSpan.FromMinutes(10);
    private const int StaleAfterIntervals = 3;
    private const int NotConnectedAfterIntervals = 5;
    private const int LowBatteryPercent = 10;

    public static ConnectionState Derive(ConnectionInputs inputs)
    {
        if (!inputs.HasSensor)
        {
            return ConnectionState.FirstRun;
        }

        if (!inputs.RadioOn)
        {
            return ConnectionState.NotConnected;
        }

        var notConnectedAfter = TimeSpan.FromTicks(Math.Max(
            minimumNotConnectedAfter.Ticks,
            inputs.Interval.Ticks * NotConnectedAfterIntervals));

        var silence = inputs.LastReadingAt is { } lastReading ? inputs.Now - lastReading : (TimeSpan?)null;

        // While the app is still trying to connect, a silence is not yet a problem to show.
        if (inputs.Attempting && (silence is null || silence >= inputs.Interval * StaleAfterIntervals))
        {
            return ConnectionState.Connecting;
        }

        if (silence is not { } quiet || quiet >= notConnectedAfter)
        {
            return ConnectionState.NotConnected;
        }

        if (quiet >= inputs.Interval * StaleAfterIntervals)
        {
            return ConnectionState.Stale;
        }

        return inputs.BatteryPercent <= LowBatteryPercent
            ? ConnectionState.LowBattery
            : ConnectionState.Normal;
    }
}
