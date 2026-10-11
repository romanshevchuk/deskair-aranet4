using Deskair.Domain.Readings;

namespace Deskair.Services.Devices;

public interface IDeviceStore
{
    Task<IReadOnlyList<DeviceInfo>> ListAsync(CancellationToken cancellationToken);

    Task SaveAsync(DeviceInfo device, CancellationToken cancellationToken);

    Task DeleteAsync(DeviceId id, CancellationToken cancellationToken);
}
