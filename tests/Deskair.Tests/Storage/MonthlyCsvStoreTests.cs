using System.IO;
using Deskair.Domain.Readings;
using Deskair.Services.Storage;
using Deskair.Storage.Readings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Storage;

public sealed class MonthlyCsvStoreTests : IDisposable
{
    private static readonly DeviceId office = new("aranet4", "AA:01");
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);
    private static readonly DateTimeOffset noon = Samples.Utc(2026, 10, 9, 12);

    private readonly TempKnownFolders folders = new();
    private readonly FakeClock clock = new(noon);
    private readonly ListLogger<MonthlyCsvStore> logger = new();
    private readonly MonthlyCsvStore store;

    public MonthlyCsvStoreTests()
    {
        store = new MonthlyCsvStore(folders, logger, clock);
    }

    // Folder name is "<providerId>-<address>" with characters that are not allowed in file names replaced (questions.md Q9).
    private string DeviceFolder => Path.Combine(folders.DataFolder, "devices", "aranet4-AA_01");

    private string MonthFile(string month) => Path.Combine(DeviceFolder, $"readings-{month}.csv");

    private void WriteFile(string month, string content)
    {
        Directory.CreateDirectory(DeviceFolder);
        File.WriteAllText(MonthFile(month), content);
    }

    private static string Row(DateTimeOffset time, double ppm, string source = "dev")
        => CsvReadingFormat.Write(Samples.Co2(time, ppm), source == "imp" ? ReadingSource.Import : ReadingSource.Device);

    private MonthlyCsvStore Reopen() => new(folders, NullLogger<MonthlyCsvStore>.Instance, clock);

    private static Task<IReadOnlyList<Reading>> All(MonthlyCsvStore from)
        => from.ReadRangeAsync(office, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, CancellationToken.None);

    public void Dispose()
    {
        store.Dispose();
        folders.Dispose();
    }

    [Fact]
    public async Task Load_gives_an_empty_history_when_the_device_has_no_folder()
    {
        Assert.Empty(await All(store));
        Assert.Null(await store.GetLastReadingTimeAsync(office, CancellationToken.None));
    }

    [Fact]
    public async Task Load_reads_every_month_file_sorted_and_skips_the_header_and_unreadable_lines()
    {
        WriteFile("2026-10", CsvReadingFormat.Header + "\n" + Row(noon.AddMinutes(2), 2) + "\nnot a row\n" + Row(noon, 1) + "\n");
        WriteFile("2026-09", CsvReadingFormat.Header + "\n" + Row(noon.AddDays(-30), 0) + "\n");

        var readings = await All(store);

        Assert.Equal(new[] { 0.0, 1.0, 2.0 }, readings.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public async Task A_cancelled_first_load_throws_and_a_later_load_reads_the_files()
    {
        WriteFile("2026-10", CsvReadingFormat.Header + "\n" + Row(noon, 1) + "\n" + Row(noon.AddMinutes(2), 2) + "\n");
        WriteFile("2026-09", CsvReadingFormat.Header + "\n" + Row(noon.AddDays(-30), 0) + "\n");
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.ReadRangeAsync(office, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, cancelled.Token));

        // Nothing was cached by the cancelled load.
        var readings = await All(store);
        Assert.Equal(new[] { 0.0, 1.0, 2.0 }, readings.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public async Task The_first_load_of_a_device_logs_how_many_readings_it_read()
    {
        WriteFile("2026-10", CsvReadingFormat.Header + "\n" + Row(noon, 1) + "\n" + Row(noon.AddMinutes(2), 2) + "\n");

        await All(store);
        await All(store);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("Loaded 2 readings for AA:01 in 0 ms.", entry.Message);
    }

    [Fact]
    public async Task Load_ignores_a_half_written_last_line()
    {
        WriteFile("2026-10", CsvReadingFormat.Header + "\n" + Row(noon, 1) + "\n" + Row(noon.AddMinutes(2), 2)[..22]);

        var readings = await All(store);

        Assert.Equal(noon, Assert.Single(readings).Timestamp);
    }

    [Fact]
    public async Task Reading_ranges_come_back_oldest_first()
    {
        var lastOfSeptember = Samples.Utc(2026, 9, 30, 23, 58);
        WriteFile("2026-10", CsvReadingFormat.Header + "\n" + Row(lastOfSeptember.AddMinutes(6), 4) + "\n");

        // Merged out of order, one reading in September and two in October, on top of what the file already holds.
        await store.MergeAsync(
            office,
            [Samples.Co2(lastOfSeptember.AddMinutes(4), 3), Samples.Co2(lastOfSeptember.AddMinutes(8), 5), Samples.Co2(lastOfSeptember, 1)],
            ReadingSource.Device,
            interval,
            CancellationToken.None);

        var readings = await store.ReadRangeAsync(office, lastOfSeptember, lastOfSeptember.AddMinutes(10), CancellationToken.None);

        Assert.Equal(new[] { 1.0, 3.0, 4.0, 5.0 }, readings.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public async Task ReadRange_is_start_inclusive_and_end_exclusive()
    {
        WriteFile("2026-10", CsvReadingFormat.Header + "\n" + Row(noon, 1) + "\n" + Row(noon.AddMinutes(2), 2) + "\n" + Row(noon.AddMinutes(4), 3) + "\n");

        var range = await store.ReadRangeAsync(office, noon, noon.AddMinutes(4), CancellationToken.None);

        Assert.Equal(new[] { 1.0, 2.0 }, range.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public async Task ReadRange_of_an_unknown_device_is_empty()
    {
        var range = await store.ReadRangeAsync(new DeviceId("aranet4", "ZZ:99"), noon, noon.AddHours(1), CancellationToken.None);

        Assert.Empty(range);
    }

    [Fact]
    public async Task LastReading_is_the_newest_stored_time()
    {
        WriteFile("2026-10", CsvReadingFormat.Header + "\n" + Row(noon, 1) + "\n" + Row(noon.AddMinutes(2), 2) + "\n");

        Assert.Equal(noon.AddMinutes(2), await store.GetLastReadingTimeAsync(office, CancellationToken.None));
    }

    [Fact]
    public async Task Merge_writes_a_new_month_file_with_a_header_and_returns_the_new_count()
    {
        var added = await store.MergeAsync(
            office, [Samples.Co2(noon, 800), Samples.Co2(noon.AddMinutes(2), 900)], ReadingSource.Device, interval, CancellationToken.None);

        Assert.Equal(2, added);
        var lines = await File.ReadAllLinesAsync(MonthFile("2026-10"), TestContext.Current.CancellationToken);
        Assert.Equal(CsvReadingFormat.Header, lines[0]);
        Assert.Equal(3, lines.Length);
    }

    [Fact]
    public async Task Merge_is_visible_straight_away_and_after_reopening_the_store()
    {
        await store.MergeAsync(office, [Samples.Co2(noon, 800)], ReadingSource.Device, interval, CancellationToken.None);

        Assert.Equal(noon, await store.GetLastReadingTimeAsync(office, CancellationToken.None));

        using var reopened = Reopen();
        Assert.Equal(800, Assert.Single(await All(reopened)).Co2Ppm);
    }

    [Fact]
    public async Task Merge_never_fills_a_slot_twice_whatever_the_source()
    {
        await store.MergeAsync(office, [Samples.Co2(noon, 800)], ReadingSource.Device, interval, CancellationToken.None);
        var before = await File.ReadAllTextAsync(MonthFile("2026-10"), TestContext.Current.CancellationToken);

        var added = await store.MergeAsync(office, [Samples.Co2(noon.AddSeconds(20), 5000)], ReadingSource.Import, interval, CancellationToken.None);

        Assert.Equal(0, added);
        Assert.Equal(before, await File.ReadAllTextAsync(MonthFile("2026-10"), TestContext.Current.CancellationToken));
        Assert.Equal(800, Assert.Single(await All(store)).Co2Ppm);
    }

    [Fact]
    public async Task Merge_only_appends_so_the_existing_bytes_of_the_file_never_change()
    {
        await store.MergeAsync(office, [Samples.Co2(noon, 800)], ReadingSource.Device, interval, CancellationToken.None);
        var before = await File.ReadAllTextAsync(MonthFile("2026-10"), TestContext.Current.CancellationToken);

        await store.MergeAsync(office, [Samples.Co2(noon.AddMinutes(2), 900)], ReadingSource.Device, interval, CancellationToken.None);

        var after = await File.ReadAllTextAsync(MonthFile("2026-10"), TestContext.Current.CancellationToken);
        Assert.StartsWith(before, after);
        Assert.True(after.Length > before.Length);
    }

    [Fact]
    public async Task Merge_snaps_the_timestamp_to_the_slot_and_marks_the_source()
    {
        await store.MergeAsync(office, [Samples.Co2(noon.AddSeconds(50), 800)], ReadingSource.Import, interval, CancellationToken.None);

        var text = await File.ReadAllTextAsync(MonthFile("2026-10"), TestContext.Current.CancellationToken);
        Assert.Contains("2026-10-09T12:00:00Z,800,", text);
        Assert.Contains(",imp", text);
    }

    [Fact]
    public async Task Merge_puts_readings_of_two_months_in_two_files()
    {
        var lastOfSeptember = Samples.Utc(2026, 9, 30, 23, 58);

        var added = await store.MergeAsync(
            office, [Samples.Co2(lastOfSeptember, 700), Samples.Co2(lastOfSeptember.AddMinutes(4), 900)], ReadingSource.Device, interval, CancellationToken.None);

        Assert.Equal(2, added);
        Assert.True(File.Exists(MonthFile("2026-09")));
        Assert.True(File.Exists(MonthFile("2026-10")));
    }

    [Fact]
    public async Task Merge_does_not_store_a_reading_with_a_missing_value()
    {
        var added = await store.MergeAsync(office, [Samples.Co2(noon, double.NaN)], ReadingSource.Device, interval, CancellationToken.None);

        Assert.Equal(0, added);
        Assert.False(File.Exists(MonthFile("2026-10")));
    }

    [Fact]
    public async Task Merge_after_a_half_written_last_line_starts_a_new_line_and_keeps_the_old_rows()
    {
        WriteFile("2026-10", CsvReadingFormat.Header + "\n" + Row(noon, 1) + "\n" + Row(noon.AddMinutes(2), 2)[..22]);

        var added = await store.MergeAsync(office, [Samples.Co2(noon.AddMinutes(4), 3)], ReadingSource.Device, interval, CancellationToken.None);

        Assert.Equal(1, added);
        using var reopened = Reopen();
        var readings = await All(reopened);
        Assert.Equal(new[] { 1.0, 3.0 }, readings.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public async Task Merge_of_nothing_adds_nothing()
    {
        Assert.Equal(0, await store.MergeAsync(office, [], ReadingSource.Device, interval, CancellationToken.None));
        Assert.False(Directory.Exists(DeviceFolder));
    }
}
