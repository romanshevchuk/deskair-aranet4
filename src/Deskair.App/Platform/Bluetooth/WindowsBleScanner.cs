using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Deskair.Services.Sensors;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace Deskair.App.Platform.Bluetooth;

/// <summary>
/// Reports every Bluetooth LE advertisement Windows hears while the sequence is being enumerated. It does not filter; providers do.
/// If Windows stops the scan (Bluetooth turned off, no adapter), the sequence ends with an <see cref="InvalidOperationException"/>.
/// Needs a manual check with a real sensor.
/// </summary>
public sealed class WindowsBleScanner : IBleScanner
{
    private const int BufferSize = 512;

    public IAsyncEnumerable<BleAdvertisement> ScanAsync(CancellationToken cancellationToken) => ScanAsync(BleScanMode.Active, cancellationToken);

    public async IAsyncEnumerable<BleAdvertisement> ScanAsync(BleScanMode mode, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // A crowded room can produce many advertisements a second; if the consumer falls behind, the oldest are dropped.
        var channel = Channel.CreateBounded<BleAdvertisement>(new BoundedChannelOptions(BufferSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = mode == BleScanMode.Passive ? BluetoothLEScanningMode.Passive : BluetoothLEScanningMode.Active
        };
        watcher.Received += (_, args) => channel.Writer.TryWrite(ToAdvertisement(args));
        watcher.Stopped += (_, args) =>
        {
            if (args.Error != BluetoothError.Success)
            {
                channel.Writer.TryComplete(new InvalidOperationException($"Windows stopped the Bluetooth scan ({args.Error})."));
            }
        };

        watcher.Start();

        try
        {
            await foreach (var advertisement in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return advertisement;
            }
        }
        finally
        {
            if (watcher.Status is BluetoothLEAdvertisementWatcherStatus.Started or BluetoothLEAdvertisementWatcherStatus.Created)
            {
                watcher.Stop();
            }

            channel.Writer.TryComplete();
        }
    }

    private static BleAdvertisement ToAdvertisement(BluetoothLEAdvertisementReceivedEventArgs args)
    {
        var manufacturerData = new Dictionary<ushort, byte[]>();

        foreach (var block in args.Advertisement.ManufacturerData)
        {
            manufacturerData[block.CompanyId] = ToBytes(block.Data);
        }

        var name = args.Advertisement.LocalName;

        return new BleAdvertisement(
            BleAddress.Format(args.BluetoothAddress),
            string.IsNullOrEmpty(name) ? null : name,
            args.RawSignalStrengthInDBm,
            manufacturerData);
    }

    private static byte[] ToBytes(IBuffer buffer)
    {
        using var reader = DataReader.FromBuffer(buffer);
        var bytes = new byte[buffer.Length];
        reader.ReadBytes(bytes);

        return bytes;
    }
}
