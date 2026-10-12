using Deskair.Domain.Readings;
using Deskair.Services.Devices;

namespace Deskair.Tests.Fakes;

/// <summary>
/// An in-memory device store. Saving replaces the device with the same id and keeps its place in the list. It is safe to use from
/// several threads (the sensor session saves from the thread pool); read the list with <see cref="Snapshot"/> while a session runs.
/// </summary>
public sealed class FakeDeviceStore : IDeviceStore
{
    private readonly Lock gate = new();

    public List<DeviceInfo> Devices { get; } = [];

    public List<DeviceInfo> Saved { get; } = [];

    public int ListCalls { get; private set; }

    public IReadOnlyList<DeviceInfo> Snapshot()
    {
        lock (gate)
        {
            return Devices.ToList();
        }
    }

    public Task<IReadOnlyList<DeviceInfo>> ListAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            ListCalls++;

            return Task.FromResult<IReadOnlyList<DeviceInfo>>(Devices.ToList());
        }
    }

    public Task SaveAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        lock (gate)
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
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(DeviceId id, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            Devices.RemoveAll(existing => existing.Id == id);
        }

        return Task.CompletedTask;
    }
}
