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
        int? battery = 80)
    {
        DateTimeOffset? last = minutesSinceReading is null ? null : now.AddMinutes(-minutesSinceReading.Value);

        return new ConnectionInputs(hasSensor, last, now, interval, radioOn, battery);
    }

    [Fact(Skip = "TODO(CONN-01a)")]
    public void Derive_is_Normal_with_a_fresh_reading()
    {
        Assert.Equal(ConnectionState.Normal, ConnectionMonitor.Derive(Inputs()));
    }

    [Fact(Skip = "TODO(CONN-01a)")]
    public void Derive_is_FirstRun_without_a_paired_sensor()
    {
        Assert.Equal(ConnectionState.FirstRun, ConnectionMonitor.Derive(Inputs(hasSensor: false, minutesSinceReading: null)));
    }

    [Fact(Skip = "TODO(CONN-01a)")]
    public void Derive_is_Stale_after_several_missed_readings()
    {
        Assert.Equal(ConnectionState.Stale, ConnectionMonitor.Derive(Inputs(minutesSinceReading: 8)));
    }

    [Fact(Skip = "TODO(CONN-01a)")]
    public void Derive_is_NotConnected_after_10_minutes_without_a_reading()
    {
        Assert.Equal(ConnectionState.NotConnected, ConnectionMonitor.Derive(Inputs(minutesSinceReading: 12)));
    }

    [Fact(Skip = "TODO(CONN-01a)")]
    public void Derive_is_NotConnected_when_Bluetooth_is_off()
    {
        Assert.Equal(ConnectionState.NotConnected, ConnectionMonitor.Derive(Inputs(radioOn: false)));
    }

    [Theory(Skip = "TODO(CONN-01a)")]
    [InlineData(10, ConnectionState.LowBattery)]
    [InlineData(5, ConnectionState.LowBattery)]
    [InlineData(11, ConnectionState.Normal)]
    [InlineData(20, ConnectionState.Normal)]
    public void Derive_is_LowBattery_at_10_percent_or_below(int battery, ConnectionState expected)
    {
        Assert.Equal(expected, ConnectionMonitor.Derive(Inputs(battery: battery)));
    }

    [Fact(Skip = "TODO(CONN-01a)")]
    public void Derive_treats_a_sensor_without_a_reading_yet_as_NotConnected()
    {
        Assert.Equal(ConnectionState.NotConnected, ConnectionMonitor.Derive(Inputs(minutesSinceReading: null)));
    }
}
