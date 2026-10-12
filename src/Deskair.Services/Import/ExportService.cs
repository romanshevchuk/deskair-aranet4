using Deskair.Domain.Readings;
using Deskair.Services.Storage;

namespace Deskair.Services.Import;

public sealed class ExportService
{
    private readonly IReadingStore readings;
    private readonly IReadingExporter exporter;
    private readonly TimeZoneInfo zone;

    public ExportService(IReadingStore readings, IReadingExporter exporter, TimeZoneInfo zone)
    {
        this.readings = readings;
        this.exporter = exporter;
        this.zone = zone;
    }

    public async Task ExportAsync(
        DeviceId device,
        DateTimeOffset start,
        DateTimeOffset end,
        Stream destination,
        CancellationToken cancellationToken)
    {
        var inRange = await readings.ReadRangeAsync(device, start, end, cancellationToken);
        var ordered = inRange.OrderBy(reading => reading.Timestamp).ToList();

        await exporter.WriteAsync(ordered, zone, destination, cancellationToken);
    }
}
