using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Platform.Notifications;

/// <summary>
/// Receives the click on a toast. It registers the COM class that <see cref="ToastIdentity"/> names, so Windows calls this process when a button is pressed and
/// starts it first when it is not running. A click that arrives before the app is ready is kept and given to the handler as soon as there is one.
/// The class is registered from a thread pool thread (the multithreaded COM apartment), so Windows can call in without a message loop; the handler runs on a COM thread.
/// </summary>
public sealed class ToastActivationServer : IDisposable
{
    private readonly ILogger<ToastActivationServer> logger;
    private readonly ConcurrentQueue<string> pending = new();
    private readonly Lock gate = new();
    private Action<string>? handler;
    private uint cookie;

    public ToastActivationServer(ILogger<ToastActivationServer> logger)
    {
        this.logger = logger;
    }

    /// <summary>Starts listening. Returns false when Windows would not take the class (the failure is logged); toasts are then shown but their buttons do nothing.</summary>
    public bool Start()
    {
        var classId = Guid.Parse(ToastIdentity.ActivatorClassId);

        // The registration belongs to the process, but the thread that makes it must be in the multithreaded apartment.
        var (result, registeredCookie) = Task.Run(() =>
        {
            var code = ToastComInterop.CoRegisterClassObject(ref classId, new ClassFactory(new Activator(Receive)), ToastComInterop.LocalServerContext, ToastComInterop.MultipleUse, out var created);

            return (code, created);
        }).GetAwaiter().GetResult();

        if (result != 0)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("Registering the toast activator failed with 0x{Result:X8}.", result);
            }

            return false;
        }

        cookie = registeredCookie;

        return true;
    }

    /// <summary>Sets who gets the arguments of each click and gives it the ones that came in early.</summary>
    public void Attach(Action<string> onActivated)
    {
        lock (gate)
        {
            handler = onActivated;
        }

        while (pending.TryDequeue(out var arguments))
        {
            onActivated(arguments);
        }
    }

    public void Dispose()
    {
        if (cookie != 0)
        {
            _ = ToastComInterop.CoRevokeClassObject(cookie);
            cookie = 0;
        }
    }

    private void Receive(string arguments)
    {
        Action<string>? current;

        lock (gate)
        {
            current = handler;
        }

        if (current is null)
        {
            pending.Enqueue(arguments);
            return;
        }

        current(arguments);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(ToastIdentity.ActivatorClassId)]
    private sealed class Activator : ToastComInterop.INotificationActivationCallback
    {
        private readonly Action<string> onActivated;

        public Activator(Action<string> onActivated)
        {
            this.onActivated = onActivated;
        }

        public void Activate(string appUserModelId, string invokedArgs, ToastComInterop.NotificationUserInputData[] data, uint dataCount)
        {
            onActivated(invokedArgs);
        }
    }

    [ComVisible(true)]
    private sealed class ClassFactory : ToastComInterop.IClassFactory
    {
        private readonly Activator activator;

        public ClassFactory(Activator activator)
        {
            this.activator = activator;
        }

        public int CreateInstance(nint outer, ref Guid interfaceId, out nint instance)
        {
            instance = 0;

            if (outer != 0)
            {
                return ToastComInterop.NoAggregation;
            }

            var unknown = Marshal.GetIUnknownForObject(activator);

            try
            {
                return Marshal.QueryInterface(unknown, in interfaceId, out instance);
            }
            finally
            {
                _ = Marshal.Release(unknown);
            }
        }

        public int LockServer(bool @lock) => 0;
    }
}
