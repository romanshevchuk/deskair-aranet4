using System.IO;
using System.Text;
using Deskair.Domain.Readings;
using Deskair.Sensors.Aranet4;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Sensors;

public sealed class PhoneExportImporterTests
{
    // Kyiv: UTC+3 (summer time) until 25 October 2026 at 04:00 local, then UTC+2.
    private static readonly TimeZoneInfo kyiv = Samples.Kyiv;

    private static readonly PhoneExportImporter importer = new();

    private static MemoryStream Text(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    private static async Task<List<Reading>> ReadTextAsync(string content)
    {
        var readings = new List<Reading>();

        await foreach (var reading in importer.ReadAsync(Text(content), kyiv, TestContext.Current.CancellationToken))
        {
            readings.Add(reading);
        }

        return readings;
    }

    private static async Task<List<Reading>> ReadXlsxAsync(Stream workbook)
    {
        var readings = new List<Reading>();

        await foreach (var reading in importer.ReadAsync(workbook, kyiv, TestContext.Current.CancellationToken))
        {
            readings.Add(reading);
        }

        return readings;
    }

    [Fact]
    public async Task Text_reads_the_columns_by_position_and_converts_local_time_to_UTC()
    {
        // 12:41:40 local in October is UTC+3, so 09:41:40 UTC.
        var readings = await ReadTextAsync("09/10/2026 12:41:40,987,22.0,51,980.7\n");

        var reading = Assert.Single(readings);
        Assert.Equal(Samples.Utc(2026, 10, 9, 9, 41, 40), reading.Timestamp);
        Assert.Equal(987, reading.Co2Ppm);
        Assert.Equal(22.0, reading.TemperatureC);
        Assert.Equal(51, reading.HumidityPct);
        Assert.Equal(980.7, reading.PressureHpa);
    }

    [Fact]
    public async Task Text_reads_tab_separated_rows_with_a_decimal_comma()
    {
        var readings = await ReadTextAsync("09/10/2026 12:43:40\t1000\t21,9\t51\t980,8\n");

        var reading = Assert.Single(readings);
        Assert.Equal(21.9, reading.TemperatureC);
        Assert.Equal(980.8, reading.PressureHpa);
    }

    [Fact]
    public async Task Text_reads_dates_day_first_so_05_10_is_5_October()
    {
        var readings = await ReadTextAsync("05/10/2026 12:00:00,800,21,45,1013\n");

        Assert.Equal(new DateOnly(2026, 10, 5), DateOnly.FromDateTime(readings[0].Timestamp.ToOffset(TimeSpan.FromHours(3)).DateTime));
    }

    [Fact]
    public async Task Text_skips_a_header_blank_lines_and_rows_with_bad_values()
    {
        var content = string.Join(
            "\n",
            "Time (DD/MM/YYYY H:mm:ss),Carbon dioxide (ppm),Temperature (°C),Relative humidity (%),Atmospheric pressure (hPa)",
            "",
            "09/10/2026 12:41:40,987,22.0,51,980.7",
            "09/10/2026 12:43:40,abc,21.9,51,980.8",
            "09/10/2026 12:45:40,990,21.9",
            "not a date,990,21.9,51,980.8",
            "09/10/2026 12:47:40,NaN,21.9,51,980.8",
            "09/10/2026 12:49:40,1010,21.8,50,980.9");

        var readings = await ReadTextAsync(content);

        Assert.Equal(new[] { 987.0, 1010.0 }, readings.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public async Task Text_accepts_one_digit_days_and_hours()
    {
        var readings = await ReadTextAsync("9/10/2026 9:05:00,800,21,45,1013\n");

        Assert.Equal(Samples.Utc(2026, 10, 9, 6, 5), readings[0].Timestamp);
    }

    [Fact]
    public async Task Text_maps_the_repeated_autumn_hour_to_its_first_occurrence()
    {
        // 03:30 on 25 October 2026 happens twice; the first one is still summer time (UTC+3).
        var readings = await ReadTextAsync("25/10/2026 03:30:00,800,21,45,1013\n");

        Assert.Equal(Samples.Utc(2026, 10, 25, 0, 30), readings[0].Timestamp);
    }

    [Fact]
    public async Task Text_maps_a_time_skipped_by_the_spring_change_with_the_offset_before_it()
    {
        // 03:30 on 29 March 2026 does not exist; clocks jump from 03:00 to 04:00. The offset before the jump (UTC+2) is used.
        var readings = await ReadTextAsync("29/03/2026 03:30:00,800,21,45,1013\n");

        Assert.Equal(Samples.Utc(2026, 3, 29, 1, 30), readings[0].Timestamp);
    }

    [Fact]
    public async Task Text_of_nothing_gives_no_readings()
    {
        Assert.Empty(await ReadTextAsync(""));
    }

    [Fact]
    public async Task Xlsx_reads_the_same_readings_as_the_text_export()
    {
        using var workbook = XlsxBuilder.Build(
            ["Time (DD/MM/YYYY H:mm:ss)", "Carbon dioxide (ppm)", "Temperature (°C)", "Relative humidity (%)", "Atmospheric pressure (hPa)"],
            ["09/10/2026 12:41:40", 987.0, 22.0, 51.0, 980.7],
            ["09/10/2026 12:43:40", 1000.0, 21.9, 51.0, 980.8]);

        var readings = await ReadXlsxAsync(workbook);

        Assert.Equal(2, readings.Count);
        Assert.Equal(Samples.Utc(2026, 10, 9, 9, 41, 40), readings[0].Timestamp);
        Assert.Equal(new[] { 987.0, 1000.0 }, readings.Select(reading => reading.Co2Ppm));
        Assert.Equal(21.9, readings[1].TemperatureC);
    }

    [Fact]
    public async Task Xlsx_reads_dates_stored_as_Excel_numbers_as_local_wall_clock_time()
    {
        // 46304.5 is 12:00 on 9 October 2026 as an Excel serial date (wall clock, no offset).
        using var workbook = XlsxBuilder.Build([46304.5, 800.0, 21.0, 45.0, 1013.0]);

        var readings = await ReadXlsxAsync(workbook);

        Assert.Equal(Samples.Utc(2026, 10, 9, 9, 0), readings[0].Timestamp);
    }

    [Fact]
    public async Task Xlsx_skips_the_header_and_bad_rows()
    {
        using var workbook = XlsxBuilder.Build(
            ["Time", "CO2", "T", "RH", "P"],
            ["09/10/2026 12:41:40", 987.0, 22.0, 51.0, 980.7],
            ["", "", "", "", ""],
            ["09/10/2026 12:45:40", "n/a", 21.9, 51.0, 980.8]);

        var readings = await ReadXlsxAsync(workbook);

        Assert.Equal(987, Assert.Single(readings).Co2Ppm);
    }

    [Fact]
    public async Task ReadAsync_chooses_the_format_by_the_file_content_not_the_extension()
    {
        using var workbook = XlsxBuilder.Build(["09/10/2026 12:41:40", 987.0, 22.0, 51.0, 980.7]);

        var readings = await ReadXlsxAsync(workbook);

        Assert.Equal(987, Assert.Single(readings).Co2Ppm);
    }

    [Fact]
    public void FileExtensions_include_text_and_excel()
    {
        Assert.Equal(new[] { ".csv", ".txt", ".xlsx" }, importer.FileExtensions);
    }

    [Fact]
    public async Task Reading_stops_when_cancelled()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in importer.ReadAsync(Text("09/10/2026 12:41:40,987,22.0,51,980.7\n"), kyiv, cancelled.Token))
            {
            }
        });
    }
}
