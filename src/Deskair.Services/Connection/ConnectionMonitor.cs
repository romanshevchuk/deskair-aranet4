namespace Deskair.Services.Connection;

/// <summary>Derives the connection state from last reading time, radio state and battery.</summary>
public sealed class ConnectionMonitor
{
    // TODO(CONN-01a) [M, haiku]
    // Spec: docs/product/behaviour-rules.md "Connection states".
    // Input : ConnectionInputs.
    // Output: the ConnectionState.
    // Rules : no sensor = FirstRun; radio off or no reading for 10 min = NotConnected;
    //         no reading for 3 intervals (about 5 min at a 2 min interval) = Stale; battery 10 % or below = LowBattery.
    // Edge  : precedence when several apply and the exact Stale limit are undecided (questions.md Q3); use FirstRun, NotConnected, Stale, LowBattery, Normal in that order until answered.
    // Done  : unskip tests Services/ConnectionMonitorTests.Derive_* and make them pass.
    public static ConnectionState Derive(ConnectionInputs inputs)
        => throw new NotImplementedException("TODO(CONN-01a)");
}
