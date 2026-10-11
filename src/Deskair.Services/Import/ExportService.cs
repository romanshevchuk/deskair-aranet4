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

    // TODO(EXPORT-01a) [S, haiku]
    // Spec: docs/engineering/data-model.md "Export CSV".
    // Input : device, range start and end, destination stream.
    // Output: writes the phone-export layout: five columns, time as YYYY-MM-DD HH:mm:ss in local time.
    // Rules : invariant culture; readings oldest first.
    // Edge  : empty range writes only the header row.
    // Done  : add Services/ExportServiceTests.Export_* with a fake store and a MemoryStream.
    public Task ExportAsync(
        DeviceId device,
        DateTimeOffset start,
        DateTimeOffset end,
        Stream destination,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(EXPORT-01a)");
}
