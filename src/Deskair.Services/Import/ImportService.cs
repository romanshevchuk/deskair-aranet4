using Deskair.Domain.Readings;
using Deskair.Services.Sensors;
using Deskair.Services.Storage;
using Deskair.Services.Sync;

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

    public async Task<ImportPreview> PreviewAsync(
        IReadingImporter importer,
        Stream source,
        DeviceId device,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        var fileReadings = await ReadAllAsync(importer, source, cancellationToken);
        var slots = SlotMerger.Merge([], fileReadings, interval);

        if (slots.Count == 0)
        {
            return new ImportPreview(0, 0, 0, null, null);
        }

        var existing = await readings.ReadRangeAsync(
            device,
            slots[0].Timestamp - interval,
            slots[^1].Timestamp + interval + TimeSpan.FromTicks(1),
            cancellationToken);
        var newSlots = SlotMerger.Merge(existing, slots, interval).Count;

        return new ImportPreview(slots.Count, newSlots, slots.Count - newSlots, slots[0].Timestamp, slots[^1].Timestamp);
    }

    public async Task<int> ImportAsync(
        IReadingImporter importer,
        Stream source,
        DeviceId device,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        var fileReadings = await ReadAllAsync(importer, source, cancellationToken);

        if (fileReadings.Count == 0)
        {
            return 0;
        }

        return await readings.MergeAsync(device, fileReadings, ReadingSource.Import, interval, cancellationToken);
    }

    private async Task<List<Reading>> ReadAllAsync(IReadingImporter importer, Stream source, CancellationToken cancellationToken)
    {
        var all = new List<Reading>();

        await foreach (var reading in importer.ReadAsync(source, zone, cancellationToken).WithCancellation(cancellationToken))
        {
            all.Add(reading);
        }

        return all;
    }
}
