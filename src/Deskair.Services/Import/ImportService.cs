using Deskair.Domain.Readings;
using Deskair.Services.Sensors;
using Deskair.Services.Storage;

namespace Deskair.Services.Import;

public sealed class ImportService
{
    private readonly IReadingStore readings;
    private readonly TimeZoneInfo zone;

    public ImportService(IReadingStore readings, TimeZoneInfo zone)
    {
        this.readings = readings;
        this.zone = zone;
    }

    // TODO(IMPORT-01a) [M, sonnet]
    // Spec: docs/engineering/sync-and-import.md "Import from the phone app" rules 3 to 4.
    // Input : importer, file stream, target device, sensor interval.
    // Output: ImportPreview (counts and date range) without writing anything.
    // Rules : count new versus already present by interval slot (SlotMerger rules).
    // Edge  : an empty file returns zeros with null dates.
    // Done  : add Services/ImportServiceTests.Preview_* with a fake importer and store.
    public Task<ImportPreview> PreviewAsync(
        IReadingImporter importer,
        Stream source,
        DeviceId device,
        TimeSpan interval,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(IMPORT-01a)");

    // TODO(IMPORT-01b) [M, sonnet]
    // Spec: docs/engineering/sync-and-import.md; docs/engineering/data-model.md "Merge rule".
    // Input : importer, file stream, target device, sensor interval.
    // Output: the number of newly filled slots.
    // Rules : merge through IReadingStore.MergeAsync with ReadingSource.Import (imports only fill empty slots).
    // Edge  : cancellation stops without a partial claim of success.
    // Done  : add Services/ImportServiceTests.Import_* with a fake importer and store.
    public Task<int> ImportAsync(
        IReadingImporter importer,
        Stream source,
        DeviceId device,
        TimeSpan interval,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(IMPORT-01b)");
}
