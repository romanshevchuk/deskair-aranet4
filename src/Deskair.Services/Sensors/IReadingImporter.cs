using Deskair.Domain.Readings;

namespace Deskair.Services.Sensors;

/// <summary>Reads a file exported by a sensor's own app into readings. Times in the file are local; the zone converts them.</summary>
public interface IReadingImporter
{
    string Id { get; }

    string DisplayNameKey { get; }

    IReadOnlyList<string> FileExtensions { get; }

    IAsyncEnumerable<Reading> ReadAsync(Stream source, TimeZoneInfo zone, CancellationToken cancellationToken);
}
