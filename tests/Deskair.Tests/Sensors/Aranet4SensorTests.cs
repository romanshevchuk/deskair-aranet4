using System.IO;
using Deskair.Domain.Readings;
using Deskair.Sensors.Aranet4;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Sensors;

public class Aranet4SensorTests
{
    private static readonly DateTimeOffset now = new(2026, 10, 12, 19, 0, 20, TimeSpan.Zero);
    private static readonly DeviceId device = new(Aranet4Provider.ProviderId, "C3DEC8FCC7C6");

    private static FakeAranet4Connection Connection(int age = 1)
        => new(Aranet4TestData.CapturedValues(), 120, age, Aranet4TestData.CurrentReadings);

    private static Aranet4Sensor Sensor(FakeAranet4Connection connection, FakeClock? clock = null)
        => new(connection, device, clock ?? new FakeClock(now));

    private static async Task<List<Reading>> ReadAll(Aranet4Sensor sensor, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var readings = new List<Reading>();

        await foreach (var reading in sensor.ReadHistoryAsync(since, cancellationToken))
        {
            readings.Add(reading);
        }

        return readings;
    }

    [Fact]
    public async Task Connecting_opens_the_address_and_reports_interval_and_battery()
    {
        var connection = Connection();

        var status = await Sensor(connection).ConnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal("C3DEC8FCC7C6", connection.ConnectedTo);
        Assert.Equal(TimeSpan.FromSeconds(120), status.Interval);
        Assert.Equal(56, status.BatteryPercent);
        Assert.Null(status.SignalDbm);
    }

    [Fact]
    public async Task A_failed_connection_surfaces_the_error()
    {
        var connection = Connection();
        connection.ConnectError = new IOException("no sensor");

        await Assert.ThrowsAsync<IOException>(() => Sensor(connection).ConnectAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_sensor_does_not_claim_a_signal_reading()
    {
        Assert.False(Sensor(Connection()).Capabilities.SignalStrength);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task The_current_reading_is_stamped_with_the_read_time_minus_the_sensors_age()
    {
        var reading = await Sensor(Connection()).ReadCurrentAsync(TestContext.Current.CancellationToken);

        Assert.Equal(now.AddSeconds(-48), reading.Timestamp);
        Assert.Equal((1121, 21.55, 51, 986.1), (reading.Co2Ppm, reading.TemperatureC, reading.HumidityPct, reading.PressureHpa));
    }

    [Fact]
    public async Task A_cancelled_current_read_does_not_touch_the_sensor()
    {
        var connection = Connection();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Sensor(connection).ReadCurrentAsync(cancelled.Token));

        Assert.Equal(0, connection.Reads);
    }

    [Fact(Timeout = 30000)]
    public async Task The_whole_captured_history_is_returned_in_order()
    {
        var connection = Connection();

        var readings = await ReadAll(Sensor(connection), DateTimeOffset.MinValue, TestContext.Current.CancellationToken);

        Assert.Equal(Aranet4Parser.ParseHistory(Aranet4TestData.CapturedPackets(), now), readings);
        Assert.Equal(5040, readings.Count);
        Assert.Equal([4, 1, 2, 3], connection.HistoryRequests.Select(request => (int)request[1]));
        Assert.All(connection.HistoryRequests, request => Assert.Equal([0x61, request[1], 0x01, 0x00], request));
    }

    [Fact(Timeout = 30000)]
    public async Task Only_the_readings_since_the_cursor_are_requested_and_returned()
    {
        var connection = Connection();
        var newestSlot = new DateTimeOffset(2026, 10, 12, 19, 0, 0, TimeSpan.Zero);
        var since = newestSlot.AddMinutes(-20);

        var readings = await ReadAll(Sensor(connection), since, TestContext.Current.CancellationToken);

        // Ten intervals back is index 5030. The request starts one index earlier, so a reading on the edge is not lost.
        Assert.All(connection.HistoryRequests, request => Assert.Equal([0x61, request[1], 0xA5, 0x13], request));
        Assert.Equal(11, readings.Count);
        Assert.Equal(since, readings[0].Timestamp);
        Assert.Equal(newestSlot, readings[^1].Timestamp);
    }

    [Fact]
    public async Task A_sensor_with_no_stored_readings_gives_an_empty_history()
    {
        var connection = new FakeAranet4Connection(
            new Dictionary<byte, ushort[]> { [Aranet4Protocol.Co2Metric] = [] },
            120,
            1,
            Aranet4TestData.CurrentReadings);

        var readings = await ReadAll(Sensor(connection), DateTimeOffset.MinValue, TestContext.Current.CancellationToken);

        Assert.Empty(readings);
        Assert.Empty(connection.HistoryRequests);
    }

    [Fact(Timeout = 30000)]
    public async Task A_dropped_connection_throws_and_returns_no_readings()
    {
        var connection = Connection();
        connection.FailOnHistoryRead = 60;
        var readings = new List<Reading>();

        var failure = await Record.ExceptionAsync(async () =>
        {
            await foreach (var reading in Sensor(connection).ReadHistoryAsync(DateTimeOffset.MinValue, TestContext.Current.CancellationToken))
            {
                readings.Add(reading);
            }
        });

        Assert.IsType<IOException>(failure);
        Assert.Empty(readings);
    }

    [Fact(Timeout = 30000)]
    public async Task A_sensor_that_stops_sending_times_out_instead_of_waiting_forever()
    {
        var connection = Connection();
        connection.StallFromHistoryRead = 10;
        var clock = new FakeClock(now);
        var task = ReadAll(Sensor(connection, clock), DateTimeOffset.MinValue, TestContext.Current.CancellationToken);

        while (!task.IsCompleted)
        {
            clock.Advance(TimeSpan.FromMilliseconds(100));
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }

        var failure = await Assert.ThrowsAsync<TimeoutException>(() => task);
        Assert.Contains("CO2", failure.Message, StringComparison.Ordinal);
    }

    [Fact(Timeout = 30000)]
    public async Task Cancelling_between_pages_stops_the_download()
    {
        var connection = Connection();
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        connection.OnRead = () =>
        {
            if (connection.HistoryReads == 5)
            {
                cancelled.Cancel();
            }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadAll(Sensor(connection), DateTimeOffset.MinValue, cancelled.Token));

        Assert.True(connection.HistoryReads <= 6);
    }

    [Fact]
    public async Task Disposing_the_sensor_disposes_the_connection()
    {
        var connection = Connection();

        await Sensor(connection).DisposeAsync();

        Assert.True(connection.Disposed);
    }
}
