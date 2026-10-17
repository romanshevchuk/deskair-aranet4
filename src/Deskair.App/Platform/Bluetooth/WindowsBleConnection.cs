using System.IO;
using Deskair.Services.Sensors;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace Deskair.App.Platform.Bluetooth;

/// <summary>
/// One GATT session with one device through the Windows Bluetooth LE APIs. Not thread-safe: use it from one flow at a time, as a sensor does.
/// A dropped connection fails the running read or write with an <see cref="IOException"/>. Needs a manual check with a real sensor.
/// </summary>
public sealed class WindowsBleConnection : IBleConnection
{
    private readonly Dictionary<(Guid Service, Guid Characteristic), GattCharacteristic> characteristics = [];
    private readonly List<GattDeviceService> services = [];
    private BluetoothLEDevice? device;

    /// <summary>Opens the device, pairs it when Windows allows pairing and it is not paired yet, and checks that its GATT services can be reached.</summary>
    public async Task ConnectAsync(string address, CancellationToken cancellationToken)
    {
        if (!BleAddress.TryParse(address, out var bluetoothAddress))
        {
            throw new ArgumentException($"'{address}' is not a Bluetooth address.", nameof(address));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var opened = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress).AsTask(cancellationToken)
            ?? throw new IOException("Windows could not open the Bluetooth device. Is it nearby and awake?");

        try
        {
            var pairing = opened.DeviceInformation.Pairing;

            if (pairing.CanPair && !pairing.IsPaired)
            {
                var paired = await pairing.PairAsync().AsTask(cancellationToken);

                if (paired.Status is not DevicePairingResultStatus.Paired and not DevicePairingResultStatus.AlreadyPaired)
                {
                    throw new IOException($"Windows could not pair with the device ({paired.Status}).");
                }
            }

            var reachable = await opened.GetGattServicesAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken);

            if (reachable.Status != GattCommunicationStatus.Success)
            {
                throw new IOException($"The device did not answer ({reachable.Status}).");
            }

            foreach (var service in reachable.Services)
            {
                service.Dispose();
            }
        }
        catch
        {
            opened.Dispose();

            throw;
        }

        device = opened;
    }

    public async Task<byte[]> ReadAsync(Guid service, Guid characteristic, CancellationToken cancellationToken)
    {
        var target = await GetCharacteristicAsync(service, characteristic, cancellationToken);
        var result = await target.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken);
        EnsureSuccess(result.Status, $"Reading {characteristic} failed");

        return ToBytes(result.Value);
    }

    public async Task WriteAsync(Guid service, Guid characteristic, byte[] value, CancellationToken cancellationToken)
    {
        var target = await GetCharacteristicAsync(service, characteristic, cancellationToken);
        var writer = new DataWriter();
        writer.WriteBytes(value);

        var status = await target.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse).AsTask(cancellationToken);
        EnsureSuccess(status, $"Writing {characteristic} failed");
    }

    public ValueTask DisposeAsync()
    {
        characteristics.Clear();

        foreach (var service in services)
        {
            service.Dispose();
        }

        services.Clear();
        device?.Dispose();
        device = null;

        return ValueTask.CompletedTask;
    }

    private static void EnsureSuccess(GattCommunicationStatus status, string message)
    {
        if (status != GattCommunicationStatus.Success)
        {
            throw new IOException($"{message} ({status}).");
        }
    }

    private static byte[] ToBytes(IBuffer buffer)
    {
        using var reader = DataReader.FromBuffer(buffer);
        var bytes = new byte[buffer.Length];
        reader.ReadBytes(bytes);

        return bytes;
    }

    private async Task<GattCharacteristic> GetCharacteristicAsync(Guid service, Guid characteristic, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (characteristics.TryGetValue((service, characteristic), out var known))
        {
            return known;
        }

        var connected = device ?? throw new InvalidOperationException("The Bluetooth connection is not open.");
        var found = await connected.GetGattServicesForUuidAsync(service, BluetoothCacheMode.Uncached).AsTask(cancellationToken);

        if (found.Status != GattCommunicationStatus.Success)
        {
            throw new IOException($"Looking up service {service} failed ({found.Status}).");
        }

        if (found.Services.Count == 0)
        {
            throw new InvalidOperationException($"The device has no service {service}.");
        }

        var gatt = found.Services[0];
        services.Add(gatt);

        var matches = await gatt.GetCharacteristicsForUuidAsync(characteristic, BluetoothCacheMode.Uncached).AsTask(cancellationToken);

        if (matches.Status != GattCommunicationStatus.Success)
        {
            throw new IOException($"Looking up characteristic {characteristic} failed ({matches.Status}).");
        }

        if (matches.Characteristics.Count == 0)
        {
            throw new InvalidOperationException($"The device has no characteristic {characteristic} in service {service}.");
        }

        var target = matches.Characteristics[0];
        characteristics[(service, characteristic)] = target;

        return target;
    }
}
