using System.Buffers.Binary;
using Deskair.Domain.Readings;
using Deskair.Services.Sync;

namespace Deskair.Sensors.Aranet4;

/// <summary>Pure parsing: bytes in, readings out, in °C and hPa.</summary>
public static class Aranet4Parser
{
    /// <summary>Reads the sensor's current-readings value. Throws <see cref="ArgumentException"/> when it is too short and <see cref="FormatException"/> when a value is marked invalid (for example, before the first measurement).</summary>
    public static Aranet4Current ParseCurrent(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < Aranet4Protocol.CurrentLength)
        {
            throw new ArgumentException($"The current readings need {Aranet4Protocol.CurrentLength} bytes, not {bytes.Length}.", nameof(bytes));
        }

        var co2 = DecodeCo2(BinaryPrimitives.ReadUInt16LittleEndian(bytes[Aranet4Protocol.CurrentCo2Offset..]));
        var temperature = DecodeTemperature(BinaryPrimitives.ReadUInt16LittleEndian(bytes[Aranet4Protocol.CurrentTemperatureOffset..]));
        var pressure = DecodePressure(BinaryPrimitives.ReadUInt16LittleEndian(bytes[Aranet4Protocol.CurrentPressureOffset..]));
        var humidity = DecodeHumidity(bytes[Aranet4Protocol.CurrentHumidityOffset]);

        if (co2 is null || temperature is null || pressure is null || humidity is null)
        {
            throw new FormatException("The sensor reports no valid current reading.");
        }

        return new Aranet4Current(
            co2.Value,
            temperature.Value,
            humidity.Value,
            pressure.Value,
            bytes[Aranet4Protocol.CurrentBatteryOffset],
            TimeSpan.FromSeconds(BinaryPrimitives.ReadUInt16LittleEndian(bytes[Aranet4Protocol.CurrentIntervalOffset..])),
            TimeSpan.FromSeconds(BinaryPrimitives.ReadUInt16LittleEndian(bytes[Aranet4Protocol.CurrentAgeOffset..])));
    }

    /// <summary>A two-byte little-endian number such as the interval, the stored count or the age. Throws <see cref="ArgumentException"/> when the value is shorter.</summary>
    public static int ParseUInt16(ReadOnlySpan<byte> bytes)
        => bytes.Length < 2
            ? throw new ArgumentException($"A two-byte value needs 2 bytes, not {bytes.Length}.", nameof(bytes))
            : BinaryPrimitives.ReadUInt16LittleEndian(bytes);

    /// <summary>The battery percentage inside the current-readings value, or null when it is outside 0 to 100. Works even when the readings themselves are invalid.</summary>
    public static int? ParseBattery(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < Aranet4Protocol.CurrentLength)
        {
            throw new ArgumentException($"The current readings need {Aranet4Protocol.CurrentLength} bytes, not {bytes.Length}.", nameof(bytes));
        }

        var percent = bytes[Aranet4Protocol.CurrentBatteryOffset];

        return percent <= 100 ? percent : null;
    }

    /// <summary>The current readings as a <see cref="Reading"/> stamped with the given time.</summary>
    public static Reading ParseCurrent(byte[] bytes, DateTimeOffset timestamp)
    {
        var current = ParseCurrent(bytes);

        return new Reading(timestamp, current.Co2Ppm, current.TemperatureC, current.HumidityPct, current.PressureHpa);
    }

    /// <summary>Reads one history page. Throws <see cref="FormatException"/> when the packet is shorter than its header or its declared values.</summary>
    public static Aranet4HistoryPage ParseHistoryPage(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < Aranet4Protocol.PageHeaderLength)
        {
            throw new FormatException("A history page is shorter than its header.");
        }

        var metric = packet[0];

        if (metric is < Aranet4Protocol.TemperatureMetric or > Aranet4Protocol.Co2Metric)
        {
            throw new FormatException($"A history page names an unknown metric ({metric}).");
        }

        var width = metric == Aranet4Protocol.HumidityMetric ? 1 : 2;
        var count = packet[Aranet4Protocol.PageCountOffset];
        var payload = packet[Aranet4Protocol.PageHeaderLength..];

        if (payload.Length < count * width)
        {
            throw new FormatException("A history page is shorter than the number of values it declares.");
        }

        var values = new ushort[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = width == 1 ? payload[index] : BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(index * width, 2));
        }

        return new Aranet4HistoryPage(
            metric,
            TimeSpan.FromSeconds(BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(Aranet4Protocol.PageIntervalOffset, 2))),
            BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(Aranet4Protocol.PageTotalOffset, 2)),
            TimeSpan.FromSeconds(BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(Aranet4Protocol.PageAgeOffset, 2))),
            BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(Aranet4Protocol.PageStartIndexOffset, 2)),
            values);
    }

    /// <summary>
    /// Joins the pages of all four metrics into readings, oldest first. The packets must be in the order they were read, and <paramref name="syncedAt"/> is the instant the first one was read.
    /// A value's time is the newest reading's time minus its distance from the newest index, snapped to the interval grid. A reading missing a metric, or with an invalid value, is left out.
    /// </summary>
    public static IReadOnlyList<Reading> ParseHistory(IReadOnlyList<byte[]> packets, DateTimeOffset syncedAt)
    {
        if (packets.Count == 0)
        {
            return [];
        }

        var byMetric = new Dictionary<byte, Dictionary<DateTimeOffset, ushort>>();
        var interval = TimeSpan.Zero;
        var newest = syncedAt;
        TimeSpan? previousAge = null;

        foreach (var packet in packets)
        {
            var page = ParseHistoryPage(packet);

            if (previousAge is null)
            {
                interval = page.Interval;
                newest = syncedAt - page.Age;

                if (interval <= TimeSpan.Zero)
                {
                    throw new FormatException("A history page reports an interval of zero.");
                }
            }
            else if (page.Interval != interval)
            {
                throw new FormatException("The pages of one download report different intervals.");
            }
            else if (page.Age < previousAge)
            {
                // The age only grows until the sensor takes its next reading, so a drop means one more reading arrived during the download.
                newest += interval;
            }

            previousAge = page.Age;

            if (!byMetric.TryGetValue(page.Metric, out var slots))
            {
                slots = [];
                byMetric[page.Metric] = slots;
            }

            for (var offset = 0; offset < page.Values.Count; offset++)
            {
                var time = newest - ((page.Total - (page.StartIndex + offset)) * interval);
                slots.TryAdd(SlotMerger.Snap(time, interval), page.Values[offset]);
            }
        }

        return Join(byMetric);
    }

    private static List<Reading> Join(Dictionary<byte, Dictionary<DateTimeOffset, ushort>> byMetric)
    {
        var readings = new List<Reading>();

        if (!byMetric.TryGetValue(Aranet4Protocol.Co2Metric, out var co2)
            || !byMetric.TryGetValue(Aranet4Protocol.TemperatureMetric, out var temperature)
            || !byMetric.TryGetValue(Aranet4Protocol.HumidityMetric, out var humidity)
            || !byMetric.TryGetValue(Aranet4Protocol.PressureMetric, out var pressure))
        {
            return readings;
        }

        foreach (var (time, rawCo2) in co2.OrderBy(pair => pair.Key))
        {
            if (temperature.TryGetValue(time, out var rawTemperature)
                && humidity.TryGetValue(time, out var rawHumidity)
                && pressure.TryGetValue(time, out var rawPressure)
                && DecodeCo2(rawCo2) is { } co2Ppm
                && DecodeTemperature(rawTemperature) is { } celsius
                && DecodeHumidity(rawHumidity) is { } percent
                && DecodePressure(rawPressure) is { } hectopascal)
            {
                readings.Add(new Reading(time, co2Ppm, celsius, percent, hectopascal));
            }
        }

        return readings;
    }

    private static double? DecodeCo2(ushort raw)
        => raw is 0 || (raw & Aranet4Protocol.InvalidCo2PressureBit) != 0 ? null : raw;

    private static double? DecodeTemperature(ushort raw)
        => raw is 0 || (raw & Aranet4Protocol.InvalidTemperatureBit) != 0 ? null : raw / Aranet4Protocol.TemperatureDivisor;

    private static double? DecodePressure(ushort raw)
        => raw is 0 || (raw & Aranet4Protocol.InvalidCo2PressureBit) != 0 ? null : raw / Aranet4Protocol.PressureDivisor;

    private static double? DecodeHumidity(ushort raw)
        => raw > Aranet4Protocol.MaxHumidityPercent ? null : raw;
}

/// <summary>The sensor's current readings with its battery, interval and the age of the measurement.</summary>
public readonly record struct Aranet4Current(double Co2Ppm, double TemperatureC, double HumidityPct, double PressureHpa, int BatteryPercent, TimeSpan Interval, TimeSpan Age);

/// <summary>One page of one metric: the sensor's interval, how many readings it stores, the age of the newest one, the 1-based index of the first value, and the raw values.</summary>
public readonly record struct Aranet4HistoryPage(byte Metric, TimeSpan Interval, int Total, TimeSpan Age, int StartIndex, IReadOnlyList<ushort> Values);
