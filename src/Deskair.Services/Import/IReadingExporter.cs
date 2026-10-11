using Deskair.Domain.Readings;

namespace Deskair.Services.Import;

/// <summary>Writes readings in the export layout. Implemented in Storage.</summary>
public interface IReadingExporter
{
    Task WriteAsync(
        IReadOnlyList<Reading> readings,
        TimeZoneInfo zone,
        Stream destination,
        CancellationToken cancellationToken);
}
