using Deskair.Domain.Readings;
using Deskair.Services.Import;

namespace Deskair.Storage.Export;

public sealed class CsvExporter : IReadingExporter
{
    // TODO(EXPORT-02a) [S, haiku]
    // Spec: docs/engineering/data-model.md "Export CSV".
    // Input : readings, time zone, destination stream.
    // Output: a header row, then one row per reading with time as YYYY-MM-DD HH:mm:ss in local time.
    // Rules : invariant culture; five columns like the phone export; UTF-8 without a byte order mark.
    // Edge  : no readings writes only the header row.
    // Done  : add Storage/CsvExporterTests.Write_* with a MemoryStream and make them pass.
    public Task WriteAsync(
        IReadOnlyList<Reading> readings,
        TimeZoneInfo zone,
        Stream destination,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(EXPORT-02a)");
}
