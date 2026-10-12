using Deskair.Domain.Readings;
using Deskair.Services.Devices;

namespace Deskair.Tests.Fakes;

/// <summary>An in-memory device store. Saving replaces the device with the same id and keeps its place in the list.</summary>
public sealed class FakeDeviceStore : IDeviceStore
{
    public List<DeviceInfo> Devices { get; } = [];

    public List<DeviceInfo> Saved { get; } = [];

    public int ListCalls { get; private set; }

    public Task<IReadOnlyList<DeviceInfo>> ListAsync(CancellationToken cancellationToken)
    {
        ListCalls++;

        return Task.FromResult<IReadOnlyList<DeviceInfo>>(Devices.ToList());
    }

    public Task SaveAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        Saved.Add(device);

        var index = Devices.FindIndex(existing => existing.Id == device.Id);

        if (index >= 0)
        {
            Devices[index] = device;
        }
        else
        {
            Devices.Add(device);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(DeviceId id, CancellationToken cancellationToken)
    {
        Devices.RemoveAll(existing => existing.Id == id);

        return Task.CompletedTask;
    }
}
