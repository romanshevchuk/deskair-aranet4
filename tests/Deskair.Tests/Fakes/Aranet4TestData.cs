using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using Deskair.Sensors.Aranet4;

namespace Deskair.Tests.Fakes;

/// <summary>Real packets and a page builder for Aranet4 tests.</summary>
public static class Aranet4TestData
{
    /// <summary>The current-readings value read from a real sensor on 2026-10-12 22:23 (1121 ppm, 21.55 °C, 51 %, 986.1 hPa, battery 56 %, 120 s, 48 s old).</summary>
    public static readonly byte[] CurrentReadings = Convert.FromHexString("6104AF01852633380278003000");

    private static string CapturePath([CallerFilePath] string thisFile = "")
        => Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "TestData", "aranet4-history-capture.txt");

    /// <summary>The 154 pages read from a real sensor on 2026-10-12 (spike S1), in the order they were read.</summary>
    public static List<byte[]> CapturedPackets()
        => File.ReadLines(CapturePath())
            .Where(line => !line.StartsWith('#') && line.Contains(','))
            .Select(line => Convert.FromHexString(line[(line.IndexOf(',') + 1)..]))
            .ToList();

    /// <summary>All values of the captured download, by metric. Index 0 is the oldest reading.</summary>
    public static Dictionary<byte, ushort[]> CapturedValues()
    {
        var byMetric = new Dictionary<byte, ushort[]>();

        foreach (var packet in CapturedPackets())
        {
            var page = Aranet4Parser.ParseHistoryPage(packet);

            if (!byMetric.TryGetValue(page.Metric, out var values))
            {
                values = new ushort[page.Total];
                byMetric[page.Metric] = values;
            }

            for (var offset = 0; offset < page.Values.Count; offset++)
            {
                values[page.StartIndex - 1 + offset] = page.Values[offset];
            }
        }

        return byMetric;
    }

    /// <summary>Builds a page the way the sensor sends it: header, then the values (one byte each for humidity, two otherwise). The interval is 120 s.</summary>
    public static byte[] Page(byte metric, int total, int age, int start, params ushort[] values)
    {
        var width = metric == Aranet4Protocol.HumidityMetric ? 1 : 2;
        var packet = new byte[Aranet4Protocol.PageHeaderLength + (values.Length * width)];
        packet[0] = metric;
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(Aranet4Protocol.PageIntervalOffset, 2), 120);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(Aranet4Protocol.PageTotalOffset, 2), (ushort)total);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(Aranet4Protocol.PageAgeOffset, 2), (ushort)age);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(Aranet4Protocol.PageStartIndexOffset, 2), (ushort)start);
        packet[Aranet4Protocol.PageCountOffset] = (byte)values.Length;

        for (var index = 0; index < values.Length; index++)
        {
            if (width == 1)
            {
                packet[Aranet4Protocol.PageHeaderLength + index] = (byte)values[index];
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(Aranet4Protocol.PageHeaderLength + (index * 2), 2), values[index]);
            }
        }

        return packet;
    }
}
