using System.IO;
using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Services.Import;
using Deskair.Services.Sensors;
using Deskair.Services.Storage;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public class ImportServiceTests
{
    private static readonly DeviceId device = new("aranet4", "AA:01");
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);
    private static readonly DateTimeOffset noon = Samples.Utc(2026, 10, 9, 12);

    private sealed class FakeImporter : IReadingImporter
    {
        private readonly IReadOnlyList<Reading> readings;

        public FakeImporter(params Reading[] readings)
        {
            this.readings = readings;
        }

        public string Id => "fake";

        public string DisplayNameKey => "Import_Fake_Name";

        public IReadOnlyList<string> FileExtensions { get; } = [".csv"];

        public async IAsyncEnumerable<Reading> ReadAsync(
            Stream source,
            TimeZoneInfo zone,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var reading in readings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return reading;
            }
        }
    }

    private readonly FakeReadingStore store = new();
    private readonly ImportService service;

    public ImportServiceTests()
    {
        service = new ImportService(store, TimeZoneInfo.Utc);
    }

    private static Reading At(int minutes, double ppm = 800) => Samples.Co2(noon.AddMinutes(minutes), ppm);

    [Fact]
    public async Task Preview_counts_new_readings_and_those_already_present_by_slot()
    {
        store.Readings.AddRange([At(0), At(2)]);
        var importer = new FakeImporter(At(0), At(2), At(4), At(6));

        var preview = await service.PreviewAsync(importer, Stream.Null, device, interval, CancellationToken.None);

        Assert.Equal(4, preview.TotalReadings);
        Assert.Equal(2, preview.NewReadings);
        Assert.Equal(2, preview.AlreadyPresent);
        Assert.Equal(noon, preview.First);
        Assert.Equal(noon.AddMinutes(6), preview.Last);
    }

    [Fact]
    public async Task Preview_matches_readings_to_slots_with_a_few_seconds_of_error()
    {
        store.Readings.Add(At(0));
        var importer = new FakeImporter(Samples.Co2(noon.AddSeconds(40), 800));

        var preview = await service.PreviewAsync(importer, Stream.Null, device, interval, CancellationToken.None);

        Assert.Equal(1, preview.AlreadyPresent);
        Assert.Equal(0, preview.NewReadings);
    }

    [Fact]
    public async Task Preview_counts_a_slot_once_and_leaves_out_readings_with_a_missing_value()
    {
        var importer = new FakeImporter(At(0), Samples.Co2(noon.AddSeconds(30), 900), At(2, double.NaN), At(4));

        var preview = await service.PreviewAsync(importer, Stream.Null, device, interval, CancellationToken.None);

        Assert.Equal(2, preview.TotalReadings);
        Assert.Equal(2, preview.NewReadings);
    }

    [Fact]
    public async Task Preview_writes_nothing()
    {
        await service.PreviewAsync(new FakeImporter(At(0)), Stream.Null, device, interval, CancellationToken.None);

        Assert.Empty(store.Merges);
        Assert.Empty(store.Readings);
    }

    [Fact]
    public async Task Preview_of_an_empty_file_is_all_zeros_without_dates()
    {
        var preview = await service.PreviewAsync(new FakeImporter(), Stream.Null, device, interval, CancellationToken.None);

        Assert.Equal(new ImportPreview(0, 0, 0, null, null), preview);
    }

    [Fact]
    public async Task Import_merges_through_the_store_as_an_import_and_returns_the_new_count()
    {
        store.Readings.Add(At(0, 500));

        var added = await service.ImportAsync(new FakeImporter(At(0, 999), At(2), At(4)), Stream.Null, device, interval, CancellationToken.None);

        Assert.Equal(2, added);
        var merge = Assert.Single(store.Merges);
        Assert.Equal((device, ReadingSource.Import, interval), (merge.Device, merge.Source, merge.Interval));
        Assert.Equal(500, store.Readings.Single(reading => reading.Timestamp == noon).Co2Ppm);
    }

    [Fact]
    public async Task Import_of_an_empty_file_adds_nothing()
    {
        var added = await service.ImportAsync(new FakeImporter(), Stream.Null, device, interval, CancellationToken.None);

        Assert.Equal(0, added);
    }

    [Fact]
    public async Task Import_that_is_cancelled_throws_and_stores_nothing()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new FakeImporter(At(0), At(2)), Stream.Null, device, interval, cancelled.Token));

        Assert.Empty(store.Readings);
    }
}
