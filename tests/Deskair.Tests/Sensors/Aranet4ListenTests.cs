using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Sensors.Aranet4;
using Deskair.Services.Sensors;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Sensors;

public class Aranet4ListenTests
{
    private static readonly DateTimeOffset now = new(2026, 10, 20, 21, 40, 0, TimeSpan.Zero);
    private static readonly DeviceId device = new(Aranet4Provider.ProviderId, "C3DEC8FCC7C6");

    // A real block: 1101 ppm, 120 s interval, battery 48 %. The age is bytes 19 and 20.
    private const string Block = "21070002000901014D04A501902639300278006100AD";

    private static BleAdvertisement Advertisement(string address, byte[] block)
        => new(address, null, -60, new Dictionary<ushort, byte[]> { [Aranet4Protocol.ManufacturerId] = block });

    private static byte[] BlockWith(byte age, byte flags = 0x21, int co2 = 1101)
    {
        var bytes = Convert.FromHexString(Block);
        bytes[0] = flags;
        bytes[8] = (byte)co2;
        bytes[9] = (byte)(co2 >> 8);
        bytes[19] = age;
        bytes[20] = 0;

        return bytes;
    }

    private static FakeAranet4Connection Connection()
        => new(Aranet4TestData.CapturedValues(), 120, 1, Aranet4TestData.CurrentReadings);

    private static async Task<List<SensorBroadcast>> Listen(IBleScanner scanner, FakeClock clock)
    {
        var sensor = new Aranet4Sensor(Connection(), scanner, device, clock);
        var heard = new List<SensorBroadcast>();

        await foreach (var broadcast in sensor.ListenAsync(TestContext.Current.CancellationToken))
        {
            heard.Add(broadcast);
        }

        return heard;
    }

    [Fact]
    public void The_sensor_says_it_announces_its_readings()
    {
        var sensor = new Aranet4Sensor(Connection(), new FakeBleScanner(), device, new FakeClock(now));

        Assert.True(sensor.Capabilities.Broadcasts);
    }

    [Fact]
    public async Task A_measurement_is_stamped_with_the_time_it_was_heard_minus_its_age_and_carries_interval_and_battery()
    {
        var scanner = new FakeBleScanner(Advertisement("C3DEC8FCC7C6", BlockWith(age: 40)));

        var heard = await Listen(scanner, new FakeClock(now));

        var broadcast = Assert.Single(heard);
        Assert.Equal(now.AddSeconds(-40), broadcast.Reading.Timestamp);
        Assert.Equal(1101, broadcast.Reading.Co2Ppm);
        Assert.Equal(TimeSpan.FromSeconds(120), broadcast.Interval);
        Assert.Equal(48, broadcast.BatteryPercent);
    }

    [Fact]
    public async Task The_same_measurement_repeated_in_every_advertisement_is_passed_on_once()
    {
        var clock = new FakeClock(now);
        var scanner = new ClockedScanner(
            clock,
            (TimeSpan.Zero, Advertisement("C3DEC8FCC7C6", BlockWith(age: 40))),
            (TimeSpan.FromSeconds(3), Advertisement("C3DEC8FCC7C6", BlockWith(age: 43))),
            (TimeSpan.FromSeconds(3), Advertisement("C3DEC8FCC7C6", BlockWith(age: 46))));

        var heard = await Listen(scanner, clock);

        Assert.Single(heard);
    }

    [Fact]
    public async Task The_next_measurement_is_passed_on_when_it_arrives()
    {
        var clock = new FakeClock(now);
        var scanner = new ClockedScanner(
            clock,
            (TimeSpan.Zero, Advertisement("C3DEC8FCC7C6", BlockWith(age: 40, co2: 1101))),
            (TimeSpan.FromSeconds(90), Advertisement("C3DEC8FCC7C6", BlockWith(age: 10, co2: 1125))));

        var heard = await Listen(scanner, clock);

        Assert.Equal([1101, 1125], heard.Select(broadcast => broadcast.Reading.Co2Ppm));
        Assert.Equal(TimeSpan.FromSeconds(120), heard[1].Reading.Timestamp - heard[0].Reading.Timestamp);
    }

    [Fact]
    public async Task Another_sensor_and_advertisements_without_measurements_are_ignored()
    {
        var scanner = new FakeBleScanner(
            Advertisement("AAAAAAAAAAAA", BlockWith(age: 40)),
            Advertisement("C3DEC8FCC7C6", BlockWith(age: 40, flags: 0x01)),
            new BleAdvertisement("C3DEC8FCC7C6", "Aranet4 0A0B3", -60, new Dictionary<ushort, byte[]>()));

        var heard = await Listen(scanner, new FakeClock(now));

        Assert.Empty(heard);
    }

    [Fact]
    public async Task The_address_is_compared_without_regard_to_case()
    {
        var scanner = new FakeBleScanner(Advertisement("c3dec8fcc7c6", BlockWith(age: 40)));

        var heard = await Listen(scanner, new FakeClock(now));

        Assert.Single(heard);
    }

    [Fact]
    public async Task Listening_does_not_connect()
    {
        var connection = Connection();
        var sensor = new Aranet4Sensor(connection, new FakeBleScanner(Advertisement("C3DEC8FCC7C6", BlockWith(age: 40))), device, new FakeClock(now));

        await foreach (var broadcast in sensor.ListenAsync(TestContext.Current.CancellationToken))
        {
            Assert.NotNull(broadcast);
        }

        Assert.Null(connection.ConnectedTo);
    }

    [Fact]
    public async Task Listening_asks_the_scanner_for_a_passive_scan()
    {
        var scanner = new ModeRecordingScanner();

        await Listen(scanner, new FakeClock(now));

        Assert.Equal(BleScanMode.Passive, scanner.Mode);
    }

    private sealed class ModeRecordingScanner : IBleScanner
    {
        public BleScanMode? Mode { get; private set; }

        public IAsyncEnumerable<BleAdvertisement> ScanAsync(CancellationToken cancellationToken) => ScanAsync(BleScanMode.Active, cancellationToken);

        public async IAsyncEnumerable<BleAdvertisement> ScanAsync(BleScanMode mode, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Mode = mode;
            await Task.CompletedTask;

            yield break;
        }
    }

    /// <summary>Reports the advertisements in order, moving the clock on by the given time before each one.</summary>
    private sealed class ClockedScanner : IBleScanner
    {
        private readonly FakeClock clock;
        private readonly (TimeSpan Wait, BleAdvertisement Advertisement)[] steps;

        public ClockedScanner(FakeClock clock, params (TimeSpan Wait, BleAdvertisement Advertisement)[] steps)
        {
            this.clock = clock;
            this.steps = steps;
        }

        public async IAsyncEnumerable<BleAdvertisement> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var (wait, advertisement) in steps)
            {
                clock.Advance(wait);
                await Task.Yield();

                yield return advertisement;
            }
        }
    }
}
