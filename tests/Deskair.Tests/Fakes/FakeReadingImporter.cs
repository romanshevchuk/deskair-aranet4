using System.IO;
using System.Runtime.CompilerServices;
using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Tests.Fakes;

/// <summary>An importer that returns the readings it was given, or throws <see cref="FormatException"/> when told the file is not valid.</summary>
public sealed class FakeReadingImporter : IReadingImporter
{
    private readonly IReadOnlyList<Reading> readings;

    public FakeReadingImporter(params Reading[] readings)
    {
        this.readings = readings;
    }

    public bool Invalid { get; init; }

    public string Id => "fake";

    public string DisplayNameKey => "Import_Fake_Name";

    public IReadOnlyList<string> FileExtensions { get; } = [".csv"];

    public async IAsyncEnumerable<Reading> ReadAsync(
        Stream source,
        TimeZoneInfo zone,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (Invalid)
        {
            throw new FormatException("Not an export file.");
        }

        foreach (var reading in readings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return reading;
        }
    }
}
