using Deskair.Sensors.Aranet4;

namespace Deskair.Tests.Sensors;

public class Aranet4AdvertisementTests
{
    // The manufacturer data block (company 0x0702) of a real sensor, captured on 2026-10-20 with Smart Home integration on and Deskair not connected.
    private const string Captured = "21070002000901014D04A501902639300278006100AD";

    [Fact]
    public void The_captured_block_is_decoded()
    {
        var current = Aranet4Parser.ParseAdvertisement(Convert.FromHexString(Captured));

        Assert.NotNull(current);
        Assert.Equal(1101, current.Value.Co2Ppm);
        Assert.Equal(21.05, current.Value.TemperatureC, 2);
        Assert.Equal(987.2, current.Value.PressureHpa, 1);
        Assert.Equal(57, current.Value.HumidityPct);
        Assert.Equal(48, current.Value.BatteryPercent);
        Assert.Equal(TimeSpan.FromSeconds(120), current.Value.Interval);
        Assert.Equal(TimeSpan.FromSeconds(97), current.Value.Age);
    }

    [Fact]
    public void A_block_without_the_integrations_flag_carries_no_measurement()
    {
        // Same bytes, first byte 0x01: Smart Home integration is off and the values in the block mean nothing.
        var bytes = Convert.FromHexString(Captured);
        bytes[0] = 0x01;

        Assert.Null(Aranet4Parser.ParseAdvertisement(bytes));
    }

    [Fact]
    public void A_block_that_is_too_short_carries_no_measurement()
    {
        Assert.Null(Aranet4Parser.ParseAdvertisement(Convert.FromHexString(Captured).AsSpan(0, 20)));
        Assert.Null(Aranet4Parser.ParseAdvertisement([]));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    public void A_block_with_an_empty_measurement_carries_no_measurement(int offset)
    {
        var bytes = Convert.FromHexString(Captured);
        bytes[offset] = 0;
        bytes[offset + 1] = 0;

        Assert.Null(Aranet4Parser.ParseAdvertisement(bytes));
    }

    [Fact]
    public void A_block_with_an_impossible_humidity_carries_no_measurement()
    {
        var bytes = Convert.FromHexString(Captured);
        bytes[14] = 200;

        Assert.Null(Aranet4Parser.ParseAdvertisement(bytes));
    }
}
