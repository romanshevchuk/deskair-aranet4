namespace Deskair.App.Composition;

/// <summary>Allows one running app per Windows user.</summary>
public sealed class SingleInstance : IDisposable
{
    private readonly Mutex mutex;
    private readonly bool owned;

    public SingleInstance(string name)
    {
        mutex = new Mutex(initiallyOwned: true, name, out owned);
    }

    public bool IsFirst => owned;

    // TODO(WIN-02a) [M, sonnet]
    // Spec: docs/engineering/architecture.md section 11 (startup order: single instance check).
    // Input : none.
    // Output: a second launch asks the first instance to show its window, then exits.
    // Rules : use a named event or pipe; keep it local to the user session.
    // Edge  : the first instance is starting up or only in the tray.
    // Done  : manual check by starting the app twice.
    public void SignalFirstInstance()
        => throw new NotImplementedException("TODO(WIN-02a)");

    public void Dispose()
    {
        if (owned)
        {
            mutex.ReleaseMutex();
        }

        mutex.Dispose();
    }
}
