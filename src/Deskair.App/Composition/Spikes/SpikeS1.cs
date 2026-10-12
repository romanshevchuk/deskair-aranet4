using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Deskair.Domain.Readings;
using Deskair.Sensors.Aranet4;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace Deskair.App.Composition.Spikes;

/// <summary>
/// Spike S1 (docs/plans/spike-s1-ble-history.md): can we read the sensor's stored history reliably and does it match the phone export?
/// Started with <c>--spike-s1</c> (optionally <c>--compare &lt;export.xlsx&gt;</c>). Throwaway: the real implementation is the ARANET and BLE cards.
/// Writes the report to the console it was started from and to <c>deskair-spike-s1.txt</c> in the current folder.
/// </summary>
public static class SpikeS1
{
    private const string OutputFolder = "artifacts/spike-s1";
    private const string ReportFileName = "report.txt";
    private const int AttachParentProcess = -1;
    private static readonly TimeSpan scanTime = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan historyWindow = TimeSpan.FromDays(7);
    private static readonly TimeSpan stallTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan transferTimeout = TimeSpan.FromMinutes(3);
    private static readonly Guid currentServiceId = Guid.Parse("0000fce0-0000-1000-8000-00805f9b34fb");
    private static readonly Guid legacyServiceId = Guid.Parse("f0cd1400-95da-4f4b-9ac8-aa55d312af0c");
    private static readonly Guid commandId = Guid.Parse("f0cd1402-95da-4f4b-9ac8-aa55d312af0c");
    private static readonly Guid totalId = Guid.Parse("f0cd2001-95da-4f4b-9ac8-aa55d312af0c");
    private static readonly Guid intervalId = Guid.Parse("f0cd2002-95da-4f4b-9ac8-aa55d312af0c");
    private static readonly Guid historyV1Id = Guid.Parse("f0cd2003-95da-4f4b-9ac8-aa55d312af0c");
    private static readonly Guid ageId = Guid.Parse("f0cd2004-95da-4f4b-9ac8-aa55d312af0c");
    private static readonly Guid historyV2Id = Guid.Parse("f0cd2005-95da-4f4b-9ac8-aa55d312af0c");
    private static readonly byte[] parameters = [SpikeS1Pages.Co2, SpikeS1Pages.Temperature, SpikeS1Pages.Humidity, SpikeS1Pages.Pressure];

    public static int Run(AppOptions options)
    {
        _ = AttachConsole(AttachParentProcess);
        var report = new StringBuilder();

        void Write(string line)
        {
            report.AppendLine(line);
            Console.WriteLine(line);
        }

        var exitCode = 1;

        try
        {
            exitCode = Task.Run(() => RunAsync(options, Write)).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Write($"SPIKE FAILED: {exception.GetType().Name}: {exception.Message}");
            Write(exception.ToString());
        }

        Directory.CreateDirectory(OutputFolder);
        var path = Path.GetFullPath(Path.Combine(OutputFolder, ReportFileName));
        File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"Report saved to {path}");

        return exitCode;
    }

    private static async Task<int> RunAsync(AppOptions options, Action<string> write)
    {
        var now = TimeProvider.System.GetLocalNow();
        write($"Spike S1 - Aranet4 history over BLE. Started {now:yyyy-MM-dd HH:mm:ss zzz}, machine time zone {TimeZoneInfo.Local.Id}.");

        var candidate = await ScanAsync(write);

        if (candidate is null)
        {
            write("RESULT: no Aranet4 found. Wake the sensor (press its button), keep it close, and run again.");

            return 2;
        }

        using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(candidate.Value.Address);

        if (device is null)
        {
            write("RESULT: Windows could not open the device.");

            return 3;
        }

        var pairing = device.DeviceInformation.Pairing;
        write($"Device: name '{device.Name}', paired {pairing.IsPaired}, can pair {pairing.CanPair}.");

        if (pairing.CanPair && !pairing.IsPaired)
        {
            write("Pairing (Windows may ask you to confirm)...");
            var result = await pairing.PairAsync();
            write($"Pairing result: {result.Status}.");
        }

        var service = await FindServiceAsync(device, write);

        if (service is null)
        {
            write("RESULT: history service not found. Is the phone app connected right now? Close it and run again.");

            return 4;
        }

        using (service)
        {
            var characteristics = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            write($"Characteristics ({characteristics.Status}): {string.Join(", ", characteristics.Characteristics.Select(c => c.Uuid.ToString()[..8]))}");
            var byId = characteristics.Characteristics.ToDictionary(c => c.Uuid);
            await DumpValuesAsync(device, characteristics.Characteristics, write);

            var total = await ReadUInt16Async(byId[totalId]);
            var interval = await ReadUInt16Async(byId[intervalId]);
            var age = await ReadUInt16Async(byId[ageId]);
            var latest = TimeProvider.System.GetUtcNow().AddSeconds(-age);
            var first = latest.AddSeconds(-(double)(total - 1) * interval);
            var capacitySpan = TimeSpan.FromSeconds((double)total * interval);

            write($"Stored readings: {total}. Interval: {interval} s. Last reading was {age} s ago.");
            write($"That covers {capacitySpan.TotalDays:F2} days: {first.ToLocalTime():yyyy-MM-dd HH:mm} to {latest.ToLocalTime():yyyy-MM-dd HH:mm} (local).");
            write($"History characteristic: {(byId.ContainsKey(historyV2Id) ? "v2 (read)" : "v1 (notify)")}.");

            if (total == 0 || interval == 0)
            {
                write("RESULT: the sensor reports no history.");

                return 5;
            }

            var wanted = (int)Math.Min(total, Math.Ceiling(historyWindow.TotalSeconds / interval));
            var start = (ushort)(total - wanted + 1);
            write($"Downloading the last {wanted} readings (indexes {start} to {total}, about {TimeSpan.FromSeconds((double)wanted * interval).TotalDays:F1} days).");

            var columns = new Dictionary<byte, ushort?[]>();
            var rawLines = new List<string>();
            var complete = true;

            foreach (var parameter in parameters)
            {
                var watch = Stopwatch.StartNew();
                var transfer = await DownloadAsync(byId, parameter, start, total, write);
                watch.Stop();
                columns[parameter] = transfer.Values;
                rawLines.AddRange(transfer.RawPackets);
                complete &= transfer.Missing == 0;
                write($"  parameter {parameter}: {transfer.Values.Length - transfer.Missing}/{transfer.Values.Length} values, {transfer.Packets} packets, {watch.Elapsed.TotalSeconds:F1} s{(transfer.Error is null ? string.Empty : $", STOPPED: {transfer.Error}")}.");
            }

            var readings = Build(columns, start, total, first, interval, out var invalid);
            write($"Complete readings: {readings.Count} of {wanted}; readings with an invalid value: {invalid}; transfer complete: {complete}.");
            WriteSamples(readings, write);
            await SaveFilesAsync(readings, rawLines, write);

            if (options.CompareFile is { } file)
            {
                await CompareAsync(file, readings, TimeSpan.FromSeconds(interval), write);
            }

            write(complete
                ? "RESULT: history downloaded completely."
                : "RESULT: history download INCOMPLETE (see the STOPPED lines above).");

            return complete ? 0 : 6;
        }
    }

    private static async Task<(ulong Address, string Name, short Rssi)?> ScanAsync(Action<string> write)
    {
        write($"Scanning for {scanTime.TotalSeconds:F0} s...");
        var found = new Dictionary<ulong, (string Name, short Rssi)>();
        var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

        watcher.Received += (_, args) =>
        {
            var advertisement = args.Advertisement;
            var isAranet = advertisement.LocalName.StartsWith("Aranet", StringComparison.OrdinalIgnoreCase)
                || advertisement.ServiceUuids.Contains(currentServiceId)
                || advertisement.ServiceUuids.Contains(legacyServiceId)
                || advertisement.ManufacturerData.Any(block => block.CompanyId == 0x0702);

            if (!isAranet)
            {
                return;
            }

            lock (found)
            {
                var name = advertisement.LocalName.Length > 0 ? advertisement.LocalName : (found.TryGetValue(args.BluetoothAddress, out var known) ? known.Name : string.Empty);
                found[args.BluetoothAddress] = (name, args.RawSignalStrengthInDBm);
            }
        };

        watcher.Start();
        await Task.Delay(scanTime);
        watcher.Stop();

        lock (found)
        {
            foreach (var (address, info) in found)
            {
                write($"  found {address:X12} '{info.Name}' {info.Rssi} dBm");
            }

            if (found.Count == 0)
            {
                return null;
            }

            var best = found.OrderByDescending(pair => pair.Value.Rssi).First();
            write($"Using {best.Key:X12}.");

            return (best.Key, best.Value.Name, best.Value.Rssi);
        }
    }

    private static async Task<GattDeviceService?> FindServiceAsync(BluetoothLEDevice device, Action<string> write)
    {
        foreach (var id in new[] { currentServiceId, legacyServiceId })
        {
            var result = await device.GetGattServicesForUuidAsync(id, BluetoothCacheMode.Uncached);
            write($"Service {id.ToString()[..8]}: {result.Status}, {result.Services.Count} found.");

            if (result.Status == GattCommunicationStatus.Success && result.Services.Count > 0)
            {
                return result.Services[0];
            }
        }

        return null;
    }

    private static async Task<TransferResult> DownloadAsync(Dictionary<Guid, GattCharacteristic> byId, byte parameter, ushort start, ushort total, Action<string> write)
    {
        var values = new ushort?[total - start + 1];
        var packets = 0;
        var rawPackets = new List<string>();
        string? error = null;
        var useV2 = byId.TryGetValue(historyV2Id, out var v2);
        var history = useV2 ? v2! : byId[historyV1Id];
        var queue = new System.Collections.Concurrent.ConcurrentQueue<byte[]>();
        Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> handler = (_, args) => queue.Enqueue(ToArray(args.CharacteristicValue));

        try
        {
            if (!useV2)
            {
                history.ValueChanged += handler;
                await history.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            }

            var request = new byte[useV2 ? 4 : 8];
            request[0] = useV2 ? (byte)0x61 : (byte)0x82;
            request[1] = parameter;

            if (useV2)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(request.AsSpan(2, 2), start);
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(request.AsSpan(4, 2), start);
                BinaryPrimitives.WriteUInt16LittleEndian(request.AsSpan(6, 2), total);
            }

            var writer = new DataWriter();
            writer.WriteBytes(request);
            var status = await byId[commandId].WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse);

            if (status != GattCommunicationStatus.Success)
            {
                return new TransferResult(values, packets, values.Length, $"command write failed ({status})", rawPackets);
            }

            var overall = Stopwatch.StartNew();
            var sinceProgress = Stopwatch.StartNew();

            while (values.Any(value => value is null))
            {
                if (sinceProgress.Elapsed > stallTimeout || overall.Elapsed > transferTimeout)
                {
                    error = $"no progress for {sinceProgress.Elapsed.TotalSeconds:F0} s at value {values.Count(value => value is not null)} of {values.Length}";
                    break;
                }

                byte[]? packet;

                if (useV2)
                {
                    var read = await history.ReadValueAsync(BluetoothCacheMode.Uncached);
                    packet = read.Status == GattCommunicationStatus.Success ? ToArray(read.Value) : null;
                }
                else if (!queue.TryDequeue(out packet))
                {
                    await Task.Delay(50);
                }

                if (packet is null)
                {
                    continue;
                }

                packets++;
                rawPackets.Add($"{parameter},{Convert.ToHexString(packet)}");

                try
                {
                    var page = useV2 ? SpikeS1Pages.ParseV2(packet, parameter) : SpikeS1Pages.ParseV1(packet, parameter);

                    if (Apply(values, start, page))
                    {
                        sinceProgress.Restart();
                    }
                    else if (useV2)
                    {
                        await Task.Delay(100);
                    }
                }
                catch (FormatException)
                {
                    if (useV2)
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            error = $"{exception.GetType().Name}: {exception.Message}";
        }
        finally
        {
            if (!useV2)
            {
                history.ValueChanged -= handler;

                try
                {
                    await history.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                }
                catch (Exception exception)
                {
                    write($"  (could not switch notifications off: {exception.Message})");
                }
            }
        }

        return new TransferResult(values, packets, values.Count(value => value is null), error, rawPackets);
    }

    private static bool Apply(ushort?[] values, ushort start, SpikeS1Page page)
    {
        var added = false;

        for (var offset = 0; offset < page.Values.Count; offset++)
        {
            var slot = page.StartIndex + offset - start;

            if (slot >= 0 && slot < values.Length && values[slot] is null)
            {
                values[slot] = page.Values[offset];
                added = true;
            }
        }

        return added;
    }

    private static List<Reading> Build(Dictionary<byte, ushort?[]> columns, ushort start, ushort total, DateTimeOffset first, ushort interval, out int invalid)
    {
        var readings = new List<Reading>();
        invalid = 0;

        for (var slot = 0; slot < total - start + 1; slot++)
        {
            var co2 = SpikeS1Pages.DecodeCo2(columns[SpikeS1Pages.Co2][slot]);
            var temperature = SpikeS1Pages.DecodeTemperature(columns[SpikeS1Pages.Temperature][slot]);
            var humidity = SpikeS1Pages.DecodeHumidity(columns[SpikeS1Pages.Humidity][slot]);
            var pressure = SpikeS1Pages.DecodePressure(columns[SpikeS1Pages.Pressure][slot]);

            if (co2 is null || temperature is null || humidity is null || pressure is null)
            {
                invalid++;

                continue;
            }

            var time = first.AddSeconds((double)(start - 1 + slot) * interval);
            readings.Add(new Reading(time, co2.Value, temperature.Value, humidity.Value, pressure.Value));
        }

        return readings;
    }

    private static void WriteSamples(List<Reading> readings, Action<string> write)
    {
        if (readings.Count == 0)
        {
            return;
        }

        write("First and last readings (local time):");

        foreach (var reading in readings.Take(3).Concat(readings.TakeLast(3)))
        {
            write(string.Create(CultureInfo.InvariantCulture, $"  {reading.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}  CO2 {reading.Co2Ppm:F0}  {reading.TemperatureC:F2} C  {reading.HumidityPct:F0} %  {reading.PressureHpa:F1} hPa"));
        }

        write(string.Create(CultureInfo.InvariantCulture, $"CO2 range {readings.Min(r => r.Co2Ppm):F0} to {readings.Max(r => r.Co2Ppm):F0} ppm; temperature {readings.Min(r => r.TemperatureC):F1} to {readings.Max(r => r.TemperatureC):F1} C."));
    }

    private static async Task CompareAsync(string file, List<Reading> sensor, TimeSpan interval, Action<string> write)
    {
        var export = new List<Reading>();
        await using var stream = File.OpenRead(file);

        await foreach (var reading in PhoneExportImporter.ReadExcelAsync(stream, TimeZoneInfo.Local, CancellationToken.None))
        {
            export.Add(reading);
        }

        var result = SpikeS1Comparison.Compare(sensor, export, interval);
        write($"Comparison with {Path.GetFileName(file)}: {result.ExportCount} export rows, {result.SensorCount} sensor readings.");
        write($"  within one interval ({interval.TotalSeconds:F0} s): {result.MatchedWithinInterval} of {result.SensorCount}; CO2 equal at the nearest row: {result.Co2Equal}.");
        write($"  time offset: median {result.MedianOffset.TotalSeconds:F0} s, largest {result.MaxOffset.TotalSeconds:F0} s.");

        var alignment = SpikeS1Comparison.Align(sensor, export, 300);
        write($"  row by row: with export row = sensor row {alignment.Shift:+#;-#;0}, {alignment.AllEqual} of {alignment.Compared} rows agree on all four values ({alignment.Co2Equal} on CO2).");

        if (export.Count > 0)
        {
            write($"  export covers {export.Min(r => r.Timestamp).ToLocalTime():yyyy-MM-dd HH:mm} to {export.Max(r => r.Timestamp).ToLocalTime():yyyy-MM-dd HH:mm} (local).");
        }
    }

    /// <summary>Reads every readable value of the sensor service and of the standard battery and device-information services, to learn the current-readings layout.</summary>
    private static async Task DumpValuesAsync(BluetoothLEDevice device, IReadOnlyList<GattCharacteristic> sensorCharacteristics, Action<string> write)
    {
        write("Current values (raw bytes, little-endian; read twice, 3 s apart, to see what changes):");
        var groups = new List<(string Name, IReadOnlyList<GattCharacteristic> Items)> { ("sensor service", sensorCharacteristics) };

        foreach (var (name, id) in new[] { ("battery service", "0000180f-0000-1000-8000-00805f9b34fb"), ("device information", "0000180a-0000-1000-8000-00805f9b34fb"), ("generic access", "00001800-0000-1000-8000-00805f9b34fb") })
        {
            var services = await device.GetGattServicesForUuidAsync(Guid.Parse(id), BluetoothCacheMode.Uncached);

            if (services.Status == GattCommunicationStatus.Success && services.Services.Count > 0)
            {
                var found = await services.Services[0].GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                groups.Add((name, found.Characteristics));
            }
        }

        for (var pass = 1; pass <= 2; pass++)
        {
            write($"  pass {pass}:");

            foreach (var (name, items) in groups)
            {
                foreach (var item in items.Where(c => c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read)))
                {
                    try
                    {
                        var result = await item.ReadValueAsync(BluetoothCacheMode.Uncached);
                        write($"    {name} {item.Uuid.ToString()[..8]}: {(result.Status == GattCommunicationStatus.Success ? Convert.ToHexString(ToArray(result.Value)) : result.Status.ToString())}");
                    }
                    catch (Exception exception)
                    {
                        write($"    {name} {item.Uuid.ToString()[..8]}: {exception.GetType().Name}");
                    }
                }
            }

            if (pass == 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    private static async Task SaveFilesAsync(List<Reading> readings, List<string> rawLines, Action<string> write)
    {
        Directory.CreateDirectory(OutputFolder);
        var csv = readings.Select(r => string.Create(CultureInfo.InvariantCulture, $"{r.Timestamp:O},{r.Co2Ppm:F0},{r.TemperatureC:F2},{r.HumidityPct:F0},{r.PressureHpa:F1}"));
        await File.WriteAllLinesAsync(Path.Combine(OutputFolder, "readings.csv"), csv.Prepend("timestamp_utc,co2_ppm,temperature_c,humidity_pct,pressure_hpa"));
        await File.WriteAllLinesAsync(Path.Combine(OutputFolder, "packets.txt"), rawLines.Prepend("# parameter,packet_hex (every packet read, in order)"));
        write($"Saved {readings.Count} readings and {rawLines.Count} raw packets to {Path.GetFullPath(OutputFolder)}.");
    }

    private static async Task<ushort> ReadUInt16Async(GattCharacteristic characteristic)
    {
        var result = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);

        if (result.Status != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException($"Could not read {characteristic.Uuid} ({result.Status}).");
        }

        return BinaryPrimitives.ReadUInt16LittleEndian(ToArray(result.Value));
    }

    private static byte[] ToArray(IBuffer buffer)
    {
        using var reader = DataReader.FromBuffer(buffer);
        var bytes = new byte[buffer.Length];
        reader.ReadBytes(bytes);

        return bytes;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsole(int processId);

    private sealed record TransferResult(ushort?[] Values, int Packets, int Missing, string? Error, List<string> RawPackets);
}
