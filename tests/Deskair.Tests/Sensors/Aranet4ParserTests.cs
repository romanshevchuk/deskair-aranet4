using System.Buffers.Binary;
using Deskair.Sensors.Aranet4;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Sensors;

public class Aranet4ParserTests
{
    private static readonly TimeSpan interval = TimeSpan.FromSeconds(120);
    private static readonly DateTimeOffset syncedAt = new(2026, 10, 12, 19, 0, 20, TimeSpan.Zero);

    private static List<byte[]> ReadCapture() => Aranet4TestData.CapturedPackets();


    [Fact]
    public void A_captured_page_is_read_with_its_header_and_values()
    {
        var page = Aranet4Parser.ParseHistoryPage(ReadCapture()[0]);

        Assert.Equal(Aranet4Protocol.Co2Metric, page.Metric);
        Assert.Equal(interval, page.Interval);
        Assert.Equal(5040, page.Total);
        Assert.Equal(1, page.StartIndex);
        Assert.Equal(117, page.Values.Count);
        Assert.Equal(1195, page.Values[0]);
    }

    [Fact]
    public void Humidity_pages_carry_one_byte_per_value()
    {
        var page = Aranet4Parser.ParseHistoryPage(Page(Aranet4Protocol.HumidityMetric, total: 3, age: 5, start: 1, 50, 51, 52));

        Assert.Equal<ushort[]>([50, 51, 52], [.. page.Values]);
    }

    [Fact]
    public void A_page_shorter_than_its_header_is_rejected()
    {
        Assert.Throws<FormatException>(() => Aranet4Parser.ParseHistoryPage(new byte[5]));
    }

    [Fact]
    public void A_page_shorter_than_its_declared_values_is_rejected()
    {
        var page = Page(Aranet4Protocol.Co2Metric, total: 3, age: 5, start: 1, 800, 900, 1000);

        Assert.Throws<FormatException>(() => Aranet4Parser.ParseHistoryPage(page.AsSpan(0, page.Length - 2)));
    }

    [Fact]
    public void A_page_for_an_unknown_metric_is_rejected()
    {
        var page = Page(Aranet4Protocol.Co2Metric, total: 1, age: 5, start: 1, 800);
        page[0] = 9;

        Assert.Throws<FormatException>(() => Aranet4Parser.ParseHistoryPage(page));
    }

    [Fact]
    public void An_empty_packet_list_gives_no_readings()
    {
        Assert.Empty(Aranet4Parser.ParseHistory([], syncedAt));
    }

    [Fact]
    public void The_captured_download_gives_every_reading_on_the_interval_grid()
    {
        var readings = Aranet4Parser.ParseHistory(ReadCapture(), syncedAt);

        Assert.Equal(5040, readings.Count);
        Assert.All(readings.Zip(readings.Skip(1)), pair => Assert.Equal(interval, pair.Second.Timestamp - pair.First.Timestamp));
        Assert.All(readings, reading => Assert.Equal(0, (reading.Timestamp.ToUnixTimeSeconds() % 120)));
    }

    [Fact]
    public void The_newest_captured_reading_is_the_sync_time_minus_its_age()
    {
        var readings = Aranet4Parser.ParseHistory(ReadCapture(), syncedAt);

        // The first captured page says the newest reading is 1 s old, so it was taken at 19:00:19, which snaps to 19:00:00.
        Assert.Equal(new DateTimeOffset(2026, 10, 12, 19, 0, 0, TimeSpan.Zero), readings[^1].Timestamp);
    }

    [Fact]
    public void The_captured_values_match_what_the_phone_app_exported()
    {
        var readings = Aranet4Parser.ParseHistory(ReadCapture(), syncedAt);

        // First and last rows of the same download, as printed by spike S1 and confirmed row by row against the phone export.
        Assert.Equal((1195, 21.80, 50, 988.0), (readings[0].Co2Ppm, readings[0].TemperatureC, readings[0].HumidityPct, readings[0].PressureHpa));
        Assert.Equal((1081, 21.30, 52, 985.9), (readings[^1].Co2Ppm, readings[^1].TemperatureC, readings[^1].HumidityPct, readings[^1].PressureHpa));
    }

    [Fact]
    public void A_value_marked_invalid_leaves_its_reading_out()
    {
        var packets = Download(total: 3, age: 10, co2: [800, 0x8001, 1000], temperature: [420, 430, 440], humidity: [40, 41, 42], pressure: [10130, 10131, 10132]);

        var readings = Aranet4Parser.ParseHistory(packets, syncedAt);

        Assert.Equal([800, 1000], readings.Select(reading => reading.Co2Ppm));
        Assert.Equal(2 * interval, readings[1].Timestamp - readings[0].Timestamp);
    }

    [Fact]
    public void A_reading_missing_one_metric_is_left_out()
    {
        var packets = Download(total: 3, age: 10, co2: [800, 900, 1000], temperature: [420, 430, 440], humidity: [40, 41, 42], pressure: [10130, 10131, 10132]);
        packets[1] = Page(Aranet4Protocol.TemperatureMetric, total: 3, age: 10, start: 2, 430, 440);

        var readings = Aranet4Parser.ParseHistory(packets, syncedAt);

        Assert.Equal([900, 1000], readings.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public void Values_are_converted_to_celsius_and_hectopascal()
    {
        var packets = Download(total: 1, age: 10, co2: [800], temperature: [430], humidity: [45], pressure: [10132]);

        var reading = Assert.Single(Aranet4Parser.ParseHistory(packets, syncedAt));

        Assert.Equal((800, 21.5, 45, 1013.2), (reading.Co2Ppm, reading.TemperatureC, reading.HumidityPct, reading.PressureHpa));
    }

    [Fact]
    public void A_reading_taken_during_the_download_does_not_shift_the_later_metrics()
    {
        // CO₂ is read first. Then the sensor (buffer full at 5 readings) takes one more, which drops the oldest and shifts every index by one.
        var co2 = Page(Aranet4Protocol.Co2Metric, total: 5, age: 100, start: 1, 810, 820, 830, 840, 850);
        var temperatureAfterNewReading = Page(Aranet4Protocol.TemperatureMetric, total: 5, age: 3, start: 1, 440, 460, 480, 500, 520);
        var humidity = Page(Aranet4Protocol.HumidityMetric, total: 5, age: 4, start: 1, 41, 42, 43, 44, 45);
        var pressure = Page(Aranet4Protocol.PressureMetric, total: 5, age: 5, start: 1, 10001, 10002, 10003, 10004, 10005);

        var readings = Aranet4Parser.ParseHistory([co2, temperatureAfterNewReading, humidity, pressure], syncedAt);

        // Temperatures belong to the readings one slot later than CO₂, so only four readings have all metrics: CO₂ 820 to 850.
        Assert.Equal([820, 830, 840, 850], readings.Select(reading => reading.Co2Ppm));
        Assert.Equal([22.0, 23.0, 24.0, 25.0], readings.Select(reading => reading.TemperatureC));
    }

    [Fact]
    public void Pages_that_disagree_on_the_interval_are_rejected()
    {
        var first = Page(Aranet4Protocol.Co2Metric, total: 2, age: 5, start: 1, 800, 900);
        var second = Page(Aranet4Protocol.TemperatureMetric, total: 2, age: 6, start: 1, 420, 430);
        BinaryPrimitives.WriteUInt16LittleEndian(second.AsSpan(Aranet4Protocol.PageIntervalOffset, 2), 300);

        Assert.Throws<FormatException>(() => Aranet4Parser.ParseHistory([first, second], syncedAt));
    }

    private static List<byte[]> Download(int total, int age, ushort[] co2, ushort[] temperature, ushort[] humidity, ushort[] pressure)
        =>
        [
            Page(Aranet4Protocol.Co2Metric, total, age, 1, co2),
            Page(Aranet4Protocol.TemperatureMetric, total, age + 1, 1, temperature),
            Page(Aranet4Protocol.HumidityMetric, total, age + 2, 1, humidity),
            Page(Aranet4Protocol.PressureMetric, total, age + 3, 1, pressure)
        ];

    private static byte[] Page(byte metric, int total, int age, int start, params ushort[] values)
        => Aranet4TestData.Page(metric, total, age, start, values);
}
