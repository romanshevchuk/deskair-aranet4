using Deskair.Services.Connection;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public class ConnectionMonitorTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 9, 12);
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);

    private static ConnectionInputs Inputs(
        bool hasSensor = true,
        int? minutesSinceReading = 1,
        bool radioOn = true,
        int? battery = 80,
        int? intervalMinutes = null,
        bool attempting = false)
    {
        DateTimeOffset? last = minutesSinceReading is null ? null : now.AddMinutes(-minutesSinceReading.Value);
        var sensorInterval = intervalMinutes is null ? interval : TimeSpan.FromMinutes(intervalMinutes.Value);

        return new ConnectionInputs(hasSensor, last, now, sensorInterval, radioOn, battery, attempting);
    }

    [Fact]
    public void Derive_is_Normal_with_a_fresh_reading()
    {
        Assert.Equal(ConnectionState.Normal, ConnectionMonitor.Derive(Inputs()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(8)]
    [InlineData(180)]
    public void Derive_is_Connecting_while_an_attempt_runs_and_the_last_reading_is_old_or_missing(int? minutesSinceReading)
    {
        Assert.Equal(ConnectionState.Connecting, ConnectionMonitor.Derive(Inputs(minutesSinceReading: minutesSinceReading, attempting: true)));
    }

    [Fact]
    public void Derive_is_Normal_during_an_attempt_when_the_last_reading_is_fresh()
    {
        Assert.Equal(ConnectionState.Normal, ConnectionMonitor.Derive(Inputs(minutesSinceReading: 1, attempting: true)));
    }

    [Fact]
    public void Derive_is_NotConnected_with_Bluetooth_off_even_during_an_attempt()
    {
        Assert.Equal(ConnectionState.NotConnected, ConnectionMonitor.Derive(Inputs(minutesSinceReading: 180, radioOn: false, attempting: true)));
    }

    [Fact]
    public void Derive_is_FirstRun_without_a_sensor_even_during_an_attempt()
    {
        Assert.Equal(ConnectionState.FirstRun, ConnectionMonitor.Derive(Inputs(hasSensor: false, minutesSinceReading: null, attempting: true)));
    }

    [Fact]
    public void Derive_is_FirstRun_without_a_paired_sensor()
    {
        Assert.Equal(ConnectionState.FirstRun, ConnectionMonitor.Derive(Inputs(hasSensor: false, minutesSinceReading: null)));
    }

    [Fact]
    public void Derive_is_Stale_after_several_missed_readings()
    {
        Assert.Equal(ConnectionState.Stale, ConnectionMonitor.Derive(Inputs(minutesSinceReading: 8)));
    }

    [Fact]
    public void Derive_is_NotConnected_after_10_minutes_without_a_reading()
    {
        Assert.Equal(ConnectionState.NotConnected, ConnectionMonitor.Derive(Inputs(minutesSinceReading: 12)));
    }

    [Fact]
    public void Derive_is_NotConnected_when_Bluetooth_is_off()
    {
        Assert.Equal(ConnectionState.NotConnected, ConnectionMonitor.Derive(Inputs(radioOn: false)));
    }

    [Theory]
    [InlineData(10, ConnectionState.LowBattery)]
    [InlineData(5, ConnectionState.LowBattery)]
    [InlineData(11, ConnectionState.Normal)]
    [InlineData(20, ConnectionState.Normal)]
    public void Derive_is_LowBattery_at_10_percent_or_below(int battery, ConnectionState expected)
    {
        Assert.Equal(expected, ConnectionMonitor.Derive(Inputs(battery: battery)));
    }

    [Theory]
    [InlineData(5, 14, ConnectionState.Normal)]
    [InlineData(5, 16, ConnectionState.Stale)]
    [InlineData(5, 24, ConnectionState.Stale)]
    [InlineData(5, 25, ConnectionState.NotConnected)]
    [InlineData(10, 29, ConnectionState.Normal)]
    [InlineData(10, 31, ConnectionState.Stale)]
    [InlineData(10, 50, ConnectionState.NotConnected)]
    public void Derive_scales_Stale_and_NotConnected_with_a_slow_sensor_interval(
        int intervalMinutes,
        int minutesSinceReading,
        ConnectionState expected)
    {
        var inputs = Inputs(minutesSinceReading: minutesSinceReading, intervalMinutes: intervalMinutes);

        Assert.Equal(expected, ConnectionMonitor.Derive(inputs));
    }

    [Fact]
    public void Derive_treats_a_sensor_without_a_reading_yet_as_NotConnected()
    {
        Assert.Equal(ConnectionState.NotConnected, ConnectionMonitor.Derive(Inputs(minutesSinceReading: null)));
    }
}
