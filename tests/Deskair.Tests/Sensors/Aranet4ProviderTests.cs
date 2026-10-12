using Deskair.Domain.Readings;
using Deskair.Sensors.Aranet4;
using Deskair.Services.Sensors;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Sensors;

public class Aranet4ProviderTests
{
    private static readonly IReadOnlyDictionary<ushort, byte[]> noManufacturerData = new Dictionary<ushort, byte[]>();

    private static BleAdvertisement Advertisement(string address, string? name, int signal = -60, ushort? company = null)
        => new(address, name, signal, company is { } id ? new Dictionary<ushort, byte[]> { [id] = [1, 2, 3] } : noManufacturerData);

    private static Aranet4Provider Provider(IBleScanner scanner)
        => new(scanner, () => new FakeAranet4Connection(Aranet4TestData.CapturedValues(), 120, 1, Aranet4TestData.CurrentReadings), new PhoneExportImporter(), new FakeClock(DateTimeOffset.UnixEpoch));

    private static async Task<List<DiscoveredSensor>> ScanAll(Aranet4Provider provider)
    {
        var found = new List<DiscoveredSensor>();

        await foreach (var sensor in provider.ScanAsync(TestContext.Current.CancellationToken))
        {
            found.Add(sensor);
        }

        return found;
    }

    [Theory]
    [InlineData("Aranet4 0A0B3", null, true)]
    [InlineData("aranet4 12345", null, true)]
    [InlineData(null, (ushort)0x0702, true)]
    [InlineData("Other", null, false)]
    [InlineData(null, (ushort)0x004C, false)]
    [InlineData(null, null, false)]
    public void An_advertisement_is_recognised_by_name_or_manufacturer_data(string? name, ushort? company, bool expected)
    {
        Assert.Equal(expected, Aranet4Provider.IsAranet4(Advertisement("A", name, company: company)));
    }

    [Fact]
    public async Task Only_sensors_are_reported()
    {
        var provider = Provider(new FakeBleScanner(
            Advertisement("PHONE", "Pixel"),
            Advertisement("C3DEC8FCC7C6", "Aranet4 0A0B3", -64),
            Advertisement("TV", null)));

        var found = await ScanAll(provider);

        var sensor = Assert.Single(found);
        Assert.Equal(new DeviceId("aranet4", "C3DEC8FCC7C6"), sensor.Id);
        Assert.Equal("Aranet4 0A0B3", sensor.Name);
        Assert.Equal(-64, sensor.SignalDbm);
    }

    [Fact]
    public async Task The_same_address_is_reported_once()
    {
        var provider = Provider(new FakeBleScanner(
            Advertisement("C3DEC8FCC7C6", "Aranet4 0A0B3", -64),
            Advertisement("c3dec8fcc7c6", null, -70, company: 0x0702),
            Advertisement("AABBCCDDEEFF", "Aranet4 99999", -80)));

        var found = await ScanAll(provider);

        Assert.Equal(["C3DEC8FCC7C6", "AABBCCDDEEFF"], found.Select(sensor => sensor.Id.Address));
    }

    [Fact]
    public async Task Scanning_stops_when_cancelled()
    {
        var provider = Provider(new FakeBleScanner(Advertisement("A", "Aranet4 1"), Advertisement("B", "Aranet4 2")));
        using var cancelled = new CancellationTokenSource();
        var found = new List<DiscoveredSensor>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var sensor in provider.ScanAsync(cancelled.Token))
            {
                found.Add(sensor);
                await cancelled.CancelAsync();
            }
        });

        Assert.Single(found);
    }

    [Fact]
    public async Task A_created_sensor_uses_a_new_connection()
    {
        var provider = Provider(new FakeBleScanner());

        await using var sensor = provider.Create(new DeviceId("aranet4", "C3DEC8FCC7C6"));
        var status = await sensor.ConnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal(56, status.BatteryPercent);
    }
}
