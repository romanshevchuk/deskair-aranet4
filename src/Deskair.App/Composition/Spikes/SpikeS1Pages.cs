using System.Buffers.Binary;

namespace Deskair.App.Composition.Spikes;

/// <summary>One page of history values as the sensor sends it: which metric, the 1-based index of the first value, and the raw values.</summary>
public readonly record struct SpikeS1Page(byte Parameter, ushort StartIndex, IReadOnlyList<ushort> Values);

/// <summary>Spike S1 only: parses history packets and decodes raw values. The real parser comes with ARANET-02a/b once the spike has confirmed the layout.</summary>
public static class SpikeS1Pages
{
    public const byte Temperature = 1;
    public const byte Humidity = 2;
    public const byte Pressure = 3;
    public const byte Co2 = 4;

    /// <summary>Notification layout: parameter, start index (2 bytes), count, values.</summary>
    public static SpikeS1Page ParseV1(ReadOnlySpan<byte> packet, byte expectedParameter)
    {
        if (packet.Length < 4)
        {
            throw new FormatException("History v1 packet is shorter than its header.");
        }

        Validate(packet[0], expectedParameter);

        return new SpikeS1Page(packet[0], BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(1, 2)), ReadValues(packet[4..], packet[3], expectedParameter));
    }

    /// <summary>Read layout: parameter, 6 bytes of header, start index (2 bytes), count, values.</summary>
    public static SpikeS1Page ParseV2(ReadOnlySpan<byte> packet, byte expectedParameter)
    {
        if (packet.Length < 10)
        {
            throw new FormatException("History v2 packet is shorter than its header.");
        }

        Validate(packet[0], expectedParameter);

        return new SpikeS1Page(packet[0], BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(7, 2)), ReadValues(packet[10..], packet[9], expectedParameter));
    }

    /// <summary>ppm, or null when the sensor marks the value as invalid.</summary>
    public static double? DecodeCo2(ushort? raw) => raw is { } value && value > 0 && (value & 0x8000) == 0 ? value : null;

    /// <summary>°C, or null when invalid.</summary>
    public static double? DecodeTemperature(ushort? raw) => raw is { } value && value > 0 && (value & 0x4000) == 0 ? value / 20.0 : null;

    /// <summary>hPa, or null when invalid.</summary>
    public static double? DecodePressure(ushort? raw) => raw is { } value && value > 0 && (value & 0x8000) == 0 ? value / 10.0 : null;

    /// <summary>Percent, or null when out of range.</summary>
    public static double? DecodeHumidity(ushort? raw) => raw is { } value && value <= 100 ? value : null;

    private static ushort[] ReadValues(ReadOnlySpan<byte> data, byte count, byte parameter)
    {
        var width = parameter == Humidity ? 1 : 2;

        if (data.Length < count * width)
        {
            throw new FormatException("History packet payload is shorter than its declared value count.");
        }

        var values = new ushort[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = width == 1 ? data[index] : BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(index * width, 2));
        }

        return values;
    }

    private static void Validate(byte actual, byte expected)
    {
        if (actual != expected)
        {
            throw new FormatException($"History packet is for parameter {actual}, not the requested {expected}.");
        }
    }
}
