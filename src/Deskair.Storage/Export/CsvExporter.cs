using System.Globalization;
using System.Text;
using Deskair.Domain.Readings;
using Deskair.Services.Import;

namespace Deskair.Storage.Export;

public sealed class CsvExporter : IReadingExporter
{
    private const string Header = "Time (YYYY-MM-DD HH:mm:ss),Carbon dioxide (ppm),Temperature (°C),Relative humidity (%),Atmospheric pressure (hPa)";
    private const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
    private const string NewLine = "\r\n";

    public async Task WriteAsync(
        IReadOnlyList<Reading> readings,
        TimeZoneInfo zone,
        Stream destination,
        CancellationToken cancellationToken)
    {
        var culture = CultureInfo.InvariantCulture;

        await using var writer = new StreamWriter(destination, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteAsync(Header + NewLine);

        foreach (var reading in readings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = string.Join(
                ',',
                TimeZoneInfo.ConvertTime(reading.Timestamp, zone).ToString(TimeFormat, culture),
                reading.Co2Ppm.ToString("0.##", culture),
                reading.TemperatureC.ToString("0.0#", culture),
                reading.HumidityPct.ToString("0.##", culture),
                reading.PressureHpa.ToString("0.0#", culture));

            await writer.WriteAsync(row + NewLine);
        }

        await writer.FlushAsync(cancellationToken);
    }
}
