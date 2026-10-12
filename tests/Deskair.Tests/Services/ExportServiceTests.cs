using System.IO;
using Deskair.Domain.Readings;
using Deskair.Services.Import;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public class ExportServiceTests
{
    private static readonly DeviceId device = new("aranet4", "AA:01");
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 9);
    private static readonly DateTimeOffset end = Samples.Utc(2026, 10, 10);

    private sealed class FakeExporter : IReadingExporter
    {
        public IReadOnlyList<Reading>? Written { get; private set; }

        public TimeZoneInfo? Zone { get; private set; }

        public Stream? Destination { get; private set; }

        public Task WriteAsync(
            IReadOnlyList<Reading> readings,
            TimeZoneInfo zone,
            Stream destination,
            CancellationToken cancellationToken)
        {
            Written = readings;
            Zone = zone;
            Destination = destination;
            return Task.CompletedTask;
        }
    }

    private readonly FakeReadingStore store = new();
    private readonly FakeExporter exporter = new();

    [Fact]
    public async Task Export_asks_the_store_for_the_device_and_range()
    {
        var service = new ExportService(store, exporter, TimeZoneInfo.Utc);

        await service.ExportAsync(device, start, end, new MemoryStream(), CancellationToken.None);

        Assert.Equal((device, start, end), store.Requested);
    }

    [Fact]
    public async Task Export_hands_the_exporter_readings_oldest_first()
    {
        store.Readings.AddRange(
        [
            Samples.Co2(start.AddHours(3), 900),
            Samples.Co2(start.AddHours(1), 700),
            Samples.Co2(start.AddHours(2), 800)
        ]);
        var service = new ExportService(store, exporter, TimeZoneInfo.Utc);

        await service.ExportAsync(device, start, end, new MemoryStream(), CancellationToken.None);

        Assert.Equal(new[] { 700.0, 800.0, 900.0 }, exporter.Written!.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public async Task Export_passes_the_zone_and_destination_through()
    {
        using var destination = new MemoryStream();
        var service = new ExportService(store, exporter, Samples.Kyiv);

        await service.ExportAsync(device, start, end, destination, CancellationToken.None);

        Assert.Same(Samples.Kyiv, exporter.Zone);
        Assert.Same(destination, exporter.Destination);
    }

    [Fact]
    public async Task Export_of_an_empty_range_still_calls_the_exporter_so_the_header_is_written()
    {
        var service = new ExportService(store, exporter, TimeZoneInfo.Utc);

        await service.ExportAsync(device, start, end, new MemoryStream(), CancellationToken.None);

        Assert.NotNull(exporter.Written);
        Assert.Empty(exporter.Written);
    }
}
