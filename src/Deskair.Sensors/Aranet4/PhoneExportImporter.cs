using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Sensors.Aranet4;

/// <summary>Reads the phone app's export: Time (DD/MM/YYYY H:mm:ss), CO₂, temperature, humidity, pressure.</summary>
public sealed class PhoneExportImporter : IReadingImporter
{
    public string Id => "aranet4-phone-export";

    public string DisplayNameKey => "Import_PhoneExport_Name";

    public IReadOnlyList<string> FileExtensions { get; } = [".csv", ".txt"];

    // TODO(IMPORT-02a) [M, haiku]
    // Spec: docs/engineering/sync-and-import.md "Import from the phone app" rules 1, 2 and 3 (text layout only).
    // Input : a CSV or tab-separated stream, the user's time zone.
    // Output: readings with UTC timestamps, one per row.
    // Rules : columns by position; parse day-first dates explicitly; local time converted with the zone.
    // Edge  : the repeated hour at the autumn change is ambiguous (see questions.md Q7); skip blank and bad rows.
    // Done  : unskip Sensors/PhoneExportImporterTests.* against a small sample file in the test folder.
    // Note  : .xlsx needs an Excel package that is not approved yet (questions.md Q8); this card covers text files only.
    public IAsyncEnumerable<Reading> ReadAsync(Stream source, TimeZoneInfo zone, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(IMPORT-02a)");
}
