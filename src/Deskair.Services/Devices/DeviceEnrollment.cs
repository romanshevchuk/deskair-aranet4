using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Messages;
using Deskair.Services.Settings;

namespace Deskair.Services.Devices;

/// <summary>
/// Adds a sensor as a room and makes it the selected one: first run and Settings → Devices both use it.
/// A sensor that is already known keeps the name and interval history it has.
/// </summary>
public sealed class DeviceEnrollment
{
    private static readonly TimeSpan defaultInterval = TimeSpan.FromMinutes(2);

    private readonly IDeviceStore store;
    private readonly DeviceRegistry registry;
    private readonly SettingsService settings;
    private readonly IMessenger messenger;

    public DeviceEnrollment(IDeviceStore store, DeviceRegistry registry, SettingsService settings, IMessenger messenger)
    {
        this.store = store;
        this.registry = registry;
        this.settings = settings;
        this.messenger = messenger;
    }

    /// <summary>
    /// Saves the room (name trimmed, 2 minute interval until the sensor reports its own), selects it, remembers the choice and sends
    /// <see cref="DeviceSelected"/>, which starts the sensor session. Throws <see cref="ArgumentException"/> for a blank name.
    /// </summary>
    public async Task<DeviceInfo> EnrollAsync(DeviceId id, string roomName, CancellationToken cancellationToken)
    {
        var name = roomName.Trim();

        if (name.Length == 0)
        {
            throw new ArgumentException("A room needs a name.", nameof(roomName));
        }

        var known = await registry.LoadAsync(cancellationToken);
        var device = known.FirstOrDefault(candidate => candidate.Id == id);

        if (device is null)
        {
            device = new DeviceInfo(id, name, null, [new IntervalChange(DateTimeOffset.UnixEpoch, defaultInterval)], null);
            await store.SaveAsync(device, cancellationToken);
            await registry.LoadAsync(cancellationToken);
        }

        registry.Select(id);
        await settings.UpdateAsync(current => current with { SelectedDevice = id }, cancellationToken);
        messenger.Send(new DeviceSelected(id));

        return device;
    }
}
