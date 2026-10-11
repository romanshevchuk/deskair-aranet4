namespace Deskair.App.Platform.Power;

/// <summary>Raises Resumed when Windows wakes from sleep, which triggers a sync.</summary>
public sealed class PowerEvents
{
    // TODO(SYNC-03a) [M, sonnet]
    // Spec: docs/engineering/architecture.md section 10 (Power); docs/engineering/sync-and-import.md.
    // Input : none.
    // Output: the Resumed event fires once per resume from sleep.
    // Rules : subscribe to the system power-mode event; unsubscribe on dispose.
    // Edge  : resume events can arrive in bursts; raise once.
    // Done  : manual check by sleeping and waking the PC.
    public event EventHandler? Resumed
    {
        add => throw new NotImplementedException("TODO(SYNC-03a)");
        remove => throw new NotImplementedException("TODO(SYNC-03a)");
    }
}
