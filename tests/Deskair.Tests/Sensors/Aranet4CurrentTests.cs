using Deskair.Sensors.Aranet4;

namespace Deskair.Tests.Sensors;

public class Aranet4CurrentTests
{
    // Read from a real sensor on 2026-10-12 22:23 (spike S1). The newest history reading of the same download was 1121 ppm, 21.55 °C, 51 %, 986.1 hPa; the battery service said 0x38.
    private const string Captured = "6104AF01852633380278003000";

    [Fact]
    public void The_captured_value_is_decoded()
    {
        var current = Aranet4Parser.ParseCurrent(Convert.FromHexString(Captured));

        Assert.Equal((1121, 21.55, 51, 986.1), (current.Co2Ppm, current.TemperatureC, current.HumidityPct, current.PressureHpa));
        Assert.Equal(56, current.BatteryPercent);
        Assert.Equal(TimeSpan.FromSeconds(120), current.Interval);
        Assert.Equal(TimeSpan.FromSeconds(48), current.Age);
    }

    [Fact]
    public void The_reading_gets_the_given_timestamp()
    {
        var time = new DateTimeOffset(2026, 10, 12, 19, 22, 0, TimeSpan.Zero);

        var reading = Aranet4Parser.ParseCurrent(Convert.FromHexString(Captured), time);

        Assert.Equal(time, reading.Timestamp);
        Assert.Equal(1121, reading.Co2Ppm);
    }

    [Fact]
    public void Too_few_bytes_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => Aranet4Parser.ParseCurrent(Convert.FromHexString("6104AF0185263338027800"), DateTimeOffset.UnixEpoch));
    }

    [Theory]
    [InlineData("0000AF01852633380278003000")]
    [InlineData("0180AF01852633380278003000")]
    [InlineData("6104AF01852665380278003000")]
    public void A_value_the_sensor_marks_invalid_is_rejected(string hex)
    {
        Assert.Throws<FormatException>(() => Aranet4Parser.ParseCurrent(Convert.FromHexString(hex)));
    }
}
