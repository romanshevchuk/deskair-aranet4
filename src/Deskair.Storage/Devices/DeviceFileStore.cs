using System.Text.Json;
using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Platform;

namespace Deskair.Storage.Devices;

/// <summary>Reads and writes <c>devices/&lt;id&gt;/device.json</c>.</summary>
public sealed class DeviceFileStore : IDeviceStore
{
    private const string FileName = "device.json";

    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IKnownFolders folders;

    public DeviceFileStore(IKnownFolders folders)
    {
        this.folders = folders;
    }

    public async Task<IReadOnlyList<DeviceInfo>> ListAsync(CancellationToken cancellationToken)
    {
        var root = DeviceFolders.Root(folders);

        if (!Directory.Exists(root))
        {
            return [];
        }

        var devices = new List<DeviceInfo>();

        foreach (var folder in Directory.GetDirectories(root))
        {
            var device = await TryReadAsync(Path.Combine(folder, FileName), cancellationToken);

            if (device is not null)
            {
                devices.Add(device);
            }
        }

        return devices;
    }

    public async Task SaveAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var folder = DeviceFolders.For(folders, device.Id);
        var path = Path.Combine(folder, FileName);
        var temporaryPath = path + ".tmp";

        Directory.CreateDirectory(folder);

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, DeviceFile.From(device), options, cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }

    public Task DeleteAsync(DeviceId id, CancellationToken cancellationToken)
    {
        var path = Path.Combine(DeviceFolders.For(folders, id), FileName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private static async Task<DeviceInfo?> TryReadAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            await using var stream = File.OpenRead(path);
            var file = await JsonSerializer.DeserializeAsync<DeviceFile>(stream, options, cancellationToken);

            return file?.ToDeviceInfo();
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed class DeviceFile
    {
        public string? ProviderId { get; init; }

        public string? Address { get; init; }

        public string? RoomName { get; init; }

        public string? RoomType { get; init; }

        public List<IntervalEntry>? IntervalHistory { get; init; }

        public DateTimeOffset? LastSyncUtc { get; init; }

        public static DeviceFile From(DeviceInfo device) => new()
        {
            ProviderId = device.Id.ProviderId,
            Address = device.Id.Address,
            RoomName = device.RoomName,
            RoomType = device.RoomType,
            IntervalHistory = device.IntervalHistory.Select(change => new IntervalEntry { From = change.From, Interval = change.Interval }).ToList(),
            LastSyncUtc = device.LastSyncUtc
        };

        public DeviceInfo? ToDeviceInfo()
        {
            if (string.IsNullOrEmpty(ProviderId) || string.IsNullOrEmpty(Address))
            {
                return null;
            }

            var history = (IntervalHistory ?? [])
                .Where(entry => entry.From is not null && entry.Interval is not null)
                .Select(entry => new IntervalChange(entry.From!.Value, entry.Interval!.Value))
                .ToList();

            return new DeviceInfo(new DeviceId(ProviderId, Address), RoomName ?? string.Empty, RoomType, history, LastSyncUtc);
        }
    }

    private sealed class IntervalEntry
    {
        public DateTimeOffset? From { get; init; }

        public TimeSpan? Interval { get; init; }
    }
}
