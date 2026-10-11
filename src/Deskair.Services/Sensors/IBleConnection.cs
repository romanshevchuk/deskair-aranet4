namespace Deskair.Services.Sensors;

/// <summary>Only what a sensor driver really needs. Add a method only when a driver needs it.</summary>
public interface IBleConnection : IAsyncDisposable
{
    Task ConnectAsync(string address, CancellationToken cancellationToken);

    Task<byte[]> ReadAsync(Guid service, Guid characteristic, CancellationToken cancellationToken);

    IAsyncEnumerable<byte[]> SubscribeAsync(Guid service, Guid characteristic, CancellationToken cancellationToken);
}
