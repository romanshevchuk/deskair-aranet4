using System.Globalization;
using Deskair.Domain.Readings;
using Deskair.Services.Storage;
using Deskair.Storage.Readings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Storage;

public class CsvReadingFormatTests
{
    private static readonly Reading sample = new(Samples.Utc(2026, 10, 9, 9, 41, 40), 987, 22.0, 51, 980.7);

    [Fact]
    public void Header_has_the_documented_columns()
    {
        Assert.Equal("ts_utc,co2_ppm,temp_c,rh_pct,pres_hpa,src", CsvReadingFormat.Header);
    }

    [Fact]
    public void Write_matches_the_documented_example_row()
    {
        Assert.Equal("2026-10-09T09:41:40Z,987,22.0,51,980.7,imp", CsvReadingFormat.Write(sample, ReadingSource.Import));
    }

    [Fact]
    public void Write_marks_sensor_readings_as_dev()
    {
        Assert.EndsWith(",dev", CsvReadingFormat.Write(sample, ReadingSource.Device));
    }

    [Theory]
    [InlineData(ReadingSource.Device)]
    [InlineData(ReadingSource.Import)]
    public void A_written_row_reads_back_the_same(ReadingSource source)
    {
        var line = CsvReadingFormat.Write(sample, source);

        var ok = CsvReadingFormat.TryParse(line, out var reading, out var parsedSource);

        Assert.True(ok);
        Assert.Equal(sample, reading);
        Assert.Equal(source, parsedSource);
    }

    [Fact]
    public void Fractional_values_survive_a_round_trip()
    {
        var precise = new Reading(Samples.Utc(2026, 1, 2, 3, 4, 5), 1068.5, 22.35, 47.5, 1013.25);

        CsvReadingFormat.TryParse(CsvReadingFormat.Write(precise, ReadingSource.Device), out var reading, out _);

        Assert.Equal(precise, reading);
    }

    [Theory]
    [InlineData("uk-UA")]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    public void Files_never_depend_on_the_current_culture(string cultureName)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

        try
        {
            var line = CsvReadingFormat.Write(sample, ReadingSource.Device);

            Assert.Equal("2026-10-09T09:41:40Z,987,22.0,51,980.7,dev", line);
            Assert.True(CsvReadingFormat.TryParse(line, out var reading, out _));
            Assert.Equal(sample, reading);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a row")]
    [InlineData("2026-10-09T09:41:40Z,987,22.0,51,980.7")]
    [InlineData("2026-10-09T09:41:40Z,987,22.0,51,980.7,dev,extra")]
    [InlineData("2026-10-09 09:41:40,987,22.0,51,980.7,dev")]
    [InlineData("2026-10-09T09:41:40Z,abc,22.0,51,980.7,dev")]
    [InlineData("2026-10-09T09:41:40Z,987,22,0,51,980.7,dev")]
    [InlineData("2026-10-09T09:41:40Z,987,22.0,51,980.7,other")]
    [InlineData("2026-10-09T09:41:40Z,NaN,22.0,51,980.7,dev")]
    public void TryParse_rejects_bad_rows_without_throwing(string line)
    {
        Assert.False(CsvReadingFormat.TryParse(line, out _, out _));
    }

    [Fact]
    public void TryParse_rejects_the_header_row()
    {
        Assert.False(CsvReadingFormat.TryParse(CsvReadingFormat.Header, out _, out _));
    }
}
