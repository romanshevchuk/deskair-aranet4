using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Deskair.Domain.Readings;
using Deskair.Services.Sensors;
using ExcelDataReader;

namespace Deskair.Sensors.Aranet4;

/// <summary>Reads the phone app's export: Time (DD/MM/YYYY H:mm:ss), CO₂, temperature, humidity, pressure. Text or .xlsx.</summary>
public sealed class PhoneExportImporter : IReadingImporter
{
    private static readonly byte[] zipSignature = "PK"u8.ToArray();

    public string Id => "aranet4-phone-export";

    public string DisplayNameKey => "Import_PhoneExport_Name";

    public IReadOnlyList<string> FileExtensions { get; } = [".csv", ".txt", ".xlsx"];

    /// <summary>Chooses the format by content: an .xlsx file is a zip archive, anything else is read as text.</summary>
    public async IAsyncEnumerable<Reading> ReadAsync(
        Stream source,
        TimeZoneInfo zone,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        if (IsZip(buffer))
        {
            await foreach (var reading in ReadExcelAsync(buffer, zone, cancellationToken))
            {
                yield return reading;
            }

            yield break;
        }

        using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryParseTextLine(line, zone, out var reading))
            {
                yield return reading;
            }
        }
    }

    /// <summary>Reads the first sheet of an .xlsx file. Header and bad rows are skipped.</summary>
    public static async IAsyncEnumerable<Reading> ReadExcelAsync(
        Stream source,
        TimeZoneInfo zone,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        RegisterCodePages();

        using var reader = ExcelReaderFactory.CreateOpenXmlReader(source);

        do
        {
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.FieldCount < PhoneExportRow.ColumnCount)
                {
                    continue;
                }

                if (TryParseExcelRow(reader, zone, out var reading))
                {
                    yield return reading;
                }
            }

            // Only the first sheet is read.
            break;
        }
        while (reader.NextResult());
    }

    /// <summary>ExcelDataReader looks up legacy code pages; they are part of the runtime but must be registered.</summary>
    private static void RegisterCodePages()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static bool IsZip(Stream buffer)
    {
        Span<byte> head = stackalloc byte[2];
        var read = buffer.Read(head);
        buffer.Position = 0;

        return read == 2 && head[0] == zipSignature[0] && head[1] == zipSignature[1];
    }

    private static bool TryParseTextLine(string line, TimeZoneInfo zone, out Reading reading)
    {
        reading = default;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var tabSeparated = line.Contains('\t');
        var cells = line.Split(tabSeparated ? '\t' : ',');

        if (cells.Length < PhoneExportRow.ColumnCount || !PhoneExportRow.TryParseTime(cells[0], out var wallClock))
        {
            return false;
        }

        var numbers = cells.Skip(1).Take(PhoneExportRow.ColumnCount - 1).ToArray();

        if (!TryParseNumbers(numbers, tabSeparated, out var values))
        {
            return false;
        }

        reading = PhoneExportRow.Build(wallClock, values[0], values[1], values[2], values[3], zone)!.Value;

        return true;
    }

    private static bool TryParseExcelRow(IExcelDataReader reader, TimeZoneInfo zone, out Reading reading)
    {
        reading = default;

        var wallClock = ExcelTime(reader.GetValue(0));

        if (wallClock is null)
        {
            return false;
        }

        var numbers = Enumerable.Range(1, PhoneExportRow.ColumnCount - 1)
            .Select(column => Convert.ToString(reader.GetValue(column), CultureInfo.InvariantCulture) ?? string.Empty)
            .ToArray();

        if (!TryParseNumbers(numbers, commaIsDecimal: false, out var values))
        {
            return false;
        }

        reading = PhoneExportRow.Build(wallClock.Value, values[0], values[1], values[2], values[3], zone)!.Value;

        return true;
    }

    private static DateTime? ExcelTime(object? cell) => cell switch
    {
        DateTime date => date,
        double serial when double.IsFinite(serial) => DateTime.FromOADate(serial),
        string text when PhoneExportRow.TryParseTime(text, out var parsed) => parsed,
        _ => null
    };

    private static bool TryParseNumbers(string[] cells, bool commaIsDecimal, out double[] values)
    {
        values = new double[cells.Length];

        for (var index = 0; index < cells.Length; index++)
        {
            if (!PhoneExportRow.TryParseNumber(cells[index], commaIsDecimal, out values[index]))
            {
                return false;
            }
        }

        return true;
    }
}
