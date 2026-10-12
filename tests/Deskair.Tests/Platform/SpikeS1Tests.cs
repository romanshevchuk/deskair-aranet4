using Deskair.App.Composition;
using Deskair.App.Composition.Spikes;
using Deskair.Domain.Readings;

namespace Deskair.Tests.Platform;

public class SpikeS1Tests
{
    [Fact]
    public void The_spike_flag_and_compare_file_are_read()
    {
        var options = AppOptions.Parse(["--spike-s1", "--compare", "export.xlsx"]);

        Assert.True(options.SpikeS1);
        Assert.Equal("export.xlsx", options.CompareFile);
    }

    [Fact]
    public void A_compare_flag_without_a_file_is_ignored()
    {
        Assert.Null(AppOptions.Parse(["--compare"]).CompareFile);
    }

    [Fact]
    public void A_v1_notification_is_parsed()
    {
        byte[] packet = [SpikeS1Pages.Co2, 0x0A, 0x00, 0x02, 0xE8, 0x03, 0xD0, 0x07];

        var page = SpikeS1Pages.ParseV1(packet, SpikeS1Pages.Co2);

        Assert.Equal(10, page.StartIndex);
        Assert.Equal([(ushort)1000, (ushort)2000], page.Values);
    }

    [Fact]
    public void A_v2_page_is_parsed_with_one_byte_humidity_values()
    {
        byte[] packet = [SpikeS1Pages.Humidity, 0, 0, 0, 0, 0, 0, 0x05, 0x00, 0x03, 40, 41, 42];

        var page = SpikeS1Pages.ParseV2(packet, SpikeS1Pages.Humidity);

        Assert.Equal(5, page.StartIndex);
        Assert.Equal([(ushort)40, (ushort)41, (ushort)42], page.Values);
    }

    [Fact]
    public void A_page_for_another_metric_is_rejected()
    {
        byte[] packet = [SpikeS1Pages.Temperature, 0x01, 0x00, 0x00];

        Assert.Throws<FormatException>(() => SpikeS1Pages.ParseV1(packet, SpikeS1Pages.Co2));
    }

    [Fact]
    public void A_page_shorter_than_its_declared_count_is_rejected()
    {
        byte[] packet = [SpikeS1Pages.Co2, 0x01, 0x00, 0x02, 0xE8, 0x03];

        Assert.Throws<FormatException>(() => SpikeS1Pages.ParseV1(packet, SpikeS1Pages.Co2));
    }

    [Fact]
    public void Raw_values_are_decoded_and_invalid_ones_are_null()
    {
        Assert.Equal(800, SpikeS1Pages.DecodeCo2(800));
        Assert.Null(SpikeS1Pages.DecodeCo2(0x8001));
        Assert.Null(SpikeS1Pages.DecodeCo2(null));
        Assert.Equal(21.5, SpikeS1Pages.DecodeTemperature(430));
        Assert.Null(SpikeS1Pages.DecodeTemperature(0x4001));
        Assert.Equal(1013.2, SpikeS1Pages.DecodePressure(10132));
        Assert.Equal(45, SpikeS1Pages.DecodeHumidity(45));
        Assert.Null(SpikeS1Pages.DecodeHumidity(200));
    }

    [Fact]
    public void Comparison_counts_readings_within_one_interval()
    {
        var start = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var interval = TimeSpan.FromMinutes(5);
        Reading[] sensor = [Make(start, 800), Make(start.AddMinutes(5), 900), Make(start.AddMinutes(60), 1000)];
        Reading[] export = [Make(start.AddSeconds(20), 800), Make(start.AddMinutes(5).AddSeconds(30), 901), Make(start.AddMinutes(30), 1000)];

        var result = SpikeS1Comparison.Compare(sensor, export, interval);

        Assert.Equal(3, result.SensorCount);
        Assert.Equal(2, result.MatchedWithinInterval);
        Assert.Equal(2, result.Co2Equal);
        Assert.Equal(TimeSpan.FromSeconds(30), result.MedianOffset);
        Assert.Equal(TimeSpan.FromMinutes(30), result.MaxOffset);
    }

    [Fact]
    public void Row_alignment_finds_the_shift_where_the_values_agree()
    {
        var start = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var sensor = Enumerable.Range(0, 20).Select(i => Make(start.AddMinutes(i * 2), 500 + (i * 10))).ToArray();
        var export = sensor.Skip(3).Select(r => r with { Timestamp = r.Timestamp.AddSeconds(40) }).ToArray();

        var result = SpikeS1Comparison.Align(sensor, export, 10);

        Assert.Equal(-3, result.Shift);
        Assert.Equal(17, result.Compared);
        Assert.Equal(17, result.AllEqual);
    }

    [Fact]
    public void Comparison_with_an_empty_export_matches_nothing()
    {
        var result = SpikeS1Comparison.Compare([Make(DateTimeOffset.UnixEpoch, 800)], [], TimeSpan.FromMinutes(5));

        Assert.Equal(0, result.MatchedWithinInterval);
        Assert.Equal(TimeSpan.Zero, result.MaxOffset);
    }

    private static Reading Make(DateTimeOffset time, double co2) => new(time, co2, 21, 40, 1000);
}
