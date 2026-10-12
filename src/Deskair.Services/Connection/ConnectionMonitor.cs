namespace Deskair.Services.Connection;

/// <summary>Derives the connection state from last reading time, radio state and battery.</summary>
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

        if (inputs.LastReadingAt is not { } lastReading)
        {
            return ConnectionState.NotConnected;
        }

        var silence = inputs.Now - lastReading;

        var notConnectedAfter = TimeSpan.FromTicks(Math.Max(
            minimumNotConnectedAfter.Ticks,
            inputs.Interval.Ticks * NotConnectedAfterIntervals));

        if (!inputs.RadioOn || silence >= notConnectedAfter)
        {
            return ConnectionState.NotConnected;
        }

        if (silence >= inputs.Interval * StaleAfterIntervals)
        {
            return ConnectionState.Stale;
        }

        return inputs.BatteryPercent <= LowBatteryPercent
            ? ConnectionState.LowBattery
            : ConnectionState.Normal;
    }
}
