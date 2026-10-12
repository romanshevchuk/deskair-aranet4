using Deskair.Domain.Readings;

namespace Deskair.Services.Devices;

/// <summary>Known devices, names, room types and the selected device.</summary>
public sealed class DeviceRegistry
{
    private static readonly TimeSpan defaultInterval = TimeSpan.FromMinutes(2);

    private readonly IDeviceStore store;
    private IReadOnlyList<DeviceInfo> known = [];

    public DeviceRegistry(IDeviceStore store)
    {
        this.store = store;
    }

    public DeviceId? Selected { get; private set; }

    public async Task<IReadOnlyList<DeviceInfo>> LoadAsync(CancellationToken cancellationToken)
    {
        known = await store.ListAsync(cancellationToken);

        if (Selected is not { } selected || known.All(device => device.Id != selected))
        {
            Selected = known.Count > 0 ? known[0].Id : null;
        }

        return known;
    }

    public TimeSpan IntervalFor(DeviceId id, DateTimeOffset at)
    {
        var history = known.FirstOrDefault(device => device.Id == id)?.IntervalHistory;

        if (history is not { Count: > 0 })
        {
            return defaultInterval;
        }

        var inEffect = history.Where(change => change.From <= at).MaxBy(change => change.From);

        return (inEffect ?? history.MinBy(change => change.From)!).Interval;
    }

    public void Select(DeviceId id)
    {
        if (known.All(device => device.Id != id))
        {
            throw new KeyNotFoundException($"Unknown device {id.ProviderId}/{id.Address}.");
        }

        Selected = id;
    }
}
