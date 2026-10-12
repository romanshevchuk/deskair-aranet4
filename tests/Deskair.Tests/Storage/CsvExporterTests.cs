using System.Globalization;
using System.IO;
using System.Text;
using Deskair.Domain.Readings;
using Deskair.Storage.Export;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Storage;

public class CsvExporterTests
{
    private const string Header = "Time (YYYY-MM-DD HH:mm:ss),Carbon dioxide (ppm),Temperature (°C),Relative humidity (%),Atmospheric pressure (hPa)";

    private static async Task<byte[]> Export(IReadOnlyList<Reading> readings, TimeZoneInfo zone)
    {
        using var stream = new MemoryStream();

        await new CsvExporter().WriteAsync(readings, zone, stream, CancellationToken.None);

        return stream.ToArray();
    }

    private static string Text(byte[] bytes) => new UTF8Encoding(false).GetString(bytes);

    [Fact]
    public async Task Write_puts_the_header_first_then_one_row_per_reading_in_local_time()
    {
        // 20:30 UTC in January is 22:30 in Kyiv.
        var readings = new[] { new Reading(Samples.Utc(2026, 1, 15, 20, 30), 987, 22.0, 51, 980.7) };

        var text = Text(await Export(readings, Samples.Kyiv));

        Assert.Equal(Header + "\r\n2026-01-15 22:30:00,987,22.0,51,980.7\r\n", text);
    }

    [Fact]
    public async Task Write_keeps_the_order_it_is_given()
    {
        var readings = new[] { Samples.Co2(Samples.Utc(2026, 10, 9, 10), 700), Samples.Co2(Samples.Utc(2026, 10, 9, 10, 2), 800) };

        var lines = Text(await Export(readings, TimeZoneInfo.Utc)).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);
        Assert.StartsWith("2026-10-09 10:00:00,700,", lines[1]);
        Assert.StartsWith("2026-10-09 10:02:00,800,", lines[2]);
    }

    [Fact]
    public async Task Write_of_no_readings_writes_only_the_header()
    {
        var text = Text(await Export([], TimeZoneInfo.Utc));

        Assert.Equal(Header + "\r\n", text);
    }

    [Fact]
    public async Task Write_is_utf8_without_a_byte_order_mark()
    {
        var bytes = await Export([], TimeZoneInfo.Utc);

        Assert.Equal((byte)'T', bytes[0]);
    }

    [Fact]
    public async Task Write_leaves_the_stream_open_for_the_caller()
    {
        using var stream = new MemoryStream();

        await new CsvExporter().WriteAsync([], TimeZoneInfo.Utc, stream, CancellationToken.None);

        Assert.True(stream.CanWrite);
    }

    [Theory]
    [InlineData("uk-UA")]
    [InlineData("de-DE")]
    public async Task Write_never_depends_on_the_current_culture(string cultureName)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

        try
        {
            var readings = new[] { new Reading(Samples.Utc(2026, 10, 9, 9, 41, 40), 1068.5, 22.35, 47.5, 1013.25) };

            var text = Text(await Export(readings, TimeZoneInfo.Utc));

            Assert.Contains("2026-10-09 09:41:40,1068.5,22.35,47.5,1013.25", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
