using System.Runtime.InteropServices;

namespace Deskair.App.Composition;

/// <summary>
/// Allows one running app per Windows user. The first instance owns a named mutex and listens on a named event; a second launch sets the event and exits,
/// and the first instance shows its window. Both names are local to the user's session, so another user's Deskair is not affected.
/// A request made while the first instance is still starting is kept until it starts listening.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const uint AnyProcess = uint.MaxValue;

    private readonly Mutex mutex;
    private readonly bool owned;
    private readonly EventWaitHandle showRequested;
    private readonly ManualResetEvent stop = new(false);
    private Task? listener;
    private bool disposed;

    public SingleInstance(string name)
    {
        // The mutex is only a marker: whoever created it first is the first instance. It is not held, so it can be released from any thread.
        mutex = new Mutex(initiallyOwned: false, name, out owned);
        showRequested = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, name + ".Show");
    }

    public bool IsFirst => owned;

    /// <summary>Asks the first instance to show its window. Also lets it take the foreground, which Windows otherwise only allows for the process that was just used.</summary>
    public void SignalFirstInstance()
    {
        AllowSetForegroundWindow(AnyProcess);
        showRequested.Set();
    }

    /// <summary>
    /// Calls <paramref name="onShowRequested"/> each time a second launch asks for the window, on a background thread until the instance is disposed.
    /// The callback must move to the UI thread itself.
    /// </summary>
    public void Listen(Action onShowRequested)
    {
        WaitHandle[] handles = [showRequested, stop];

        listener = Task.Run(() =>
        {
            while (WaitHandle.WaitAny(handles) == 0)
            {
                onShowRequested();
            }
        });
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        stop.Set();
        listener?.Wait(TimeSpan.FromSeconds(2));
        stop.Dispose();
        showRequested.Dispose();
        mutex.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint processId);
}
