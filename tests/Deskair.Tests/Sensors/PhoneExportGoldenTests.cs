using System.IO;
using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Sensors.Aranet4;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Sensors;

/// <summary>Reads a real export from the phone app (TestData/Aranet4_0A0B3_Export_Example.xlsx, exported 12 Oct 2026, day-first dates).</summary>
public sealed class PhoneExportGoldenTests
{
    private static readonly PhoneExportImporter importer = new();

    private static string ExamplePath([CallerFilePath] string thisFile = "")
        => Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "TestData", "Aranet4_0A0B3_Export_Example.xlsx");

    private static async Task<List<Reading>> ReadExampleAsync()
    {
        var readings = new List<Reading>();
        await using var file = File.OpenRead(ExamplePath());

        await foreach (var reading in importer.ReadAsync(file, Samples.Kyiv, TestContext.Current.CancellationToken))
        {
            readings.Add(reading);
        }

        return readings;
    }

    [Fact]
    public async Task Every_row_of_the_real_export_is_read()
    {
        var readings = await ReadExampleAsync();

        Assert.Equal(720, readings.Count);
    }

    [Fact]
    public async Task The_first_and_last_rows_have_the_exported_values_in_utc()
    {
        var readings = await ReadExampleAsync();

        // 09/10/2026 12:41:40 in Kyiv (UTC+3) and 10/10/2026 12:39:27.
        Assert.Equal(new Reading(Samples.Utc(2026, 10, 9, 9, 41, 40), 987, 22.0, 51, 980.7), readings[0]);
        Assert.Equal(new Reading(Samples.Utc(2026, 10, 10, 9, 39, 27), 772, 20.9, 44, 978.7), readings[^1]);
    }

    [Fact]
    public async Task The_real_export_covers_one_day_in_time_order()
    {
        var readings = await ReadExampleAsync();

        Assert.Equal(readings.OrderBy(reading => reading.Timestamp), readings);
        Assert.InRange((readings[^1].Timestamp - readings[0].Timestamp).TotalHours, 23.9, 24.0);
    }
}
