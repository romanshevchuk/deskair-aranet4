using Deskair.Domain.Readings;

namespace Deskair.Services.Devices;

/// <summary>Known devices, names, room types and the selected device.</summary>
public sealed class DeviceRegistry
{
    private readonly IDeviceStore store;

    public DeviceRegistry(IDeviceStore store)
    {
        this.store = store;
    }

    public DeviceId? Selected { get; private set; }

    // TODO(DEV-01a) [S, haiku]
    // Spec: docs/engineering/architecture.md section 6 (DeviceRegistry); docs/engineering/data-model.md.
    // Input : none (reads through IDeviceStore).
    // Output: all known devices; the first becomes Selected when none is selected yet.
    // Rules : keep a cached copy; do not call the store twice for the same load.
    // Edge  : no devices returns an empty list and Selected stays null (First run state).
    // Done  : add Services/DeviceRegistryTests with a fake IDeviceStore and make them pass.
    public Task<IReadOnlyList<DeviceInfo>> LoadAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(DEV-01a)");

    // TODO(DEV-01b) [S, haiku]
    // Spec: docs/engineering/architecture.md section 6 (DeviceRegistry).
    // Input : a device id that is already known.
    // Output: Selected is updated; unknown id throws KeyNotFoundException.
    // Rules : no persistence of the selection here (decision: see questions.md Q4).
    // Edge  : selecting the already selected device does nothing.
    // Done  : add Services/DeviceRegistryTests.Select_* and make them pass.
    public void Select(DeviceId id)
        => throw new NotImplementedException("TODO(DEV-01b)");
}
