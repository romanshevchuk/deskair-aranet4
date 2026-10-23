using Microsoft.Extensions.DependencyInjection;

namespace Deskair.App.Composition;

public static class ServiceProviderShutdown
{
    /// <summary>
    /// Disposes the container the way the generic host did. Some services (the Bluetooth connection) can only be disposed asynchronously, and the container's own
    /// synchronous Dispose throws for them. The wait runs on a pool thread, because the UI thread has stopped and cannot run a continuation.
    /// </summary>
    public static void Dispose(ServiceProvider provider)
    {
        Task.Run(() => provider.DisposeAsync().AsTask()).GetAwaiter().GetResult();
    }
}
