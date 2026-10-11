using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Platform;

namespace Deskair.Storage.Devices;

/// <summary>Reads and writes <c>devices/&lt;id&gt;/device.json</c>.</summary>
public sealed class DeviceFileStore : IDeviceStore
{
    private readonly IKnownFolders folders;

    public DeviceFileStore(IKnownFolders folders)
    {
        this.folders = folders;
    }

    // TODO(STORE-03a) [S, haiku]
    // Spec: docs/engineering/data-model.md (device.json).
    // Input : none.
    // Output: every device.json found under devices/.
    // Rules : invariant culture; ignore a folder whose device.json cannot be read, and log nothing here.
    // Edge  : a missing devices folder returns an empty list.
    // Done  : add Storage/DeviceFileStoreTests.List_* using a temp folder and make them pass.
    public Task<IReadOnlyList<DeviceInfo>> ListAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(STORE-03a)");

    // TODO(STORE-03b) [S, haiku]
    // Spec: docs/engineering/data-model.md "Safety" (temp file then rename).
    // Input : a DeviceInfo.
    // Output: device.json written for that device.
    // Rules : create the folder if needed; write a temp file in the same folder and rename over the old file.
    // Edge  : a failure leaves the previous device.json untouched.
    // Done  : add Storage/DeviceFileStoreTests.Save_* and make them pass.
    public Task SaveAsync(DeviceInfo device, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(STORE-03b)");

    // TODO(STORE-03c) [S, haiku]
    // Spec: docs/engineering/data-model.md; AGENTS.md (never delete readings).
    // Input : a device id.
    // Output: removes device.json only; readings files stay.
    // Rules : readings are never deleted by this method.
    // Edge  : a missing file does nothing.
    // Done  : add Storage/DeviceFileStoreTests.Delete_* and make them pass.
    public Task DeleteAsync(DeviceId id, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(STORE-03c)");
}
