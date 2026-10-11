using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Sensors.Simulated;
using Deskair.Services.Sensors;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Sensors;

public class SimulatedSensorTests
{
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);
    private static readonly DateTimeOffset noon = Samples.Utc(2026, 10, 9, 12);

    private static SimulatedSensor Create(FakeClock clock) => new(clock, interval);

    [Fact]
    public void ReadingAt_is_deterministic()
    {
        Assert.Equal(SimulatedSensor.ReadingAt(noon), SimulatedSensor.ReadingAt(noon));
    }

    [Fact]
    public async Task ReadCurrent_returns_a_reading_on_the_interval_grid()
    {
        var clock = new FakeClock(noon.AddSeconds(95));
        await using var sensor = Create(clock);

        var reading = await sensor.ReadCurrentAsync(CancellationToken.None);

        Assert.Equal(noon, reading.Timestamp);
        Assert.Equal(0, reading.Timestamp.UtcTicks % interval.Ticks);
    }

    [Fact]
    public async Task ReadHistory_returns_one_reading_per_slot_in_order()
    {
        var clock = new FakeClock(noon);
        await using var sensor = Create(clock);

        var history = new List<Reading>();

        await foreach (var reading in sensor.ReadHistoryAsync(noon.AddHours(-1), CancellationToken.None))
        {
            history.Add(reading);
        }

        Assert.Equal(31, history.Count);
        Assert.Equal(noon.AddHours(-1), history[0].Timestamp);
        Assert.Equal(noon, history[^1].Timestamp);
        Assert.Equal(history.OrderBy(reading => reading.Timestamp), history);
    }

    [Fact]
    public async Task ReadHistory_stops_when_cancelled()
    {
        var clock = new FakeClock(noon);
        await using var sensor = Create(clock);
        using var cancellation = new CancellationTokenSource();

        var count = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var reading in sensor.ReadHistoryAsync(noon.AddDays(-1), cancellation.Token))
            {
                count++;
                await cancellation.CancelAsync();
            }
        });

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ConnectAsync_reports_the_interval()
    {
        await using var sensor = Create(new FakeClock(noon));

        var status = await sensor.ConnectAsync(CancellationToken.None);

        Assert.Equal(interval, status.Interval);
        Assert.NotNull(status.BatteryPercent);
    }

    [Fact]
    public void Constructor_rejects_a_non_positive_interval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedSensor(new FakeClock(noon), TimeSpan.Zero));
    }

    [Fact]
    public void A_day_of_readings_visits_every_zone()
    {
        var zones = Enumerable.Range(0, 24 * 30)
            .Select(step => SimulatedSensor.ReadingAt(Samples.Utc(2026, 10, 9).AddMinutes(step * 2)))
            .Select(reading => ZoneClassifier.Classify(reading.Co2Ppm))
            .Distinct()
            .ToList();

        Assert.Contains(Zone.Good, zones);
        Assert.Contains(Zone.Elevated, zones);
        Assert.Contains(Zone.High, zones);
    }

    [Fact]
    public async Task Provider_finds_one_sensor_and_creates_it()
    {
        var provider = new SimulatedProvider(new FakeClock(noon));

        var found = new List<DiscoveredSensor>();

        await foreach (var sensor in provider.ScanAsync(CancellationToken.None))
        {
            found.Add(sensor);
        }

        Assert.Single(found);
        Assert.Equal(SimulatedProvider.ProviderId, found[0].Id.ProviderId);
        await using var created = provider.Create(found[0].Id);
        Assert.True(created.Capabilities.History);
    }

    [Fact]
    public void Provider_returns_resource_keys_not_text()
    {
        var provider = new SimulatedProvider(new FakeClock(noon));

        Assert.Matches("^Sensor_", provider.DisplayNameKey);
        Assert.All(provider.OnboardingStepKeys, key => Assert.Matches("^Sensor_", key));
    }
}
