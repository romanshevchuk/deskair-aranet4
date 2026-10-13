namespace Deskair.Services.Platform;

/// <summary>One kind of file a dialog offers: a description such as "CSV file" and the extensions with their dots (".csv").</summary>
public sealed record FileTypeFilter(string Description, IReadOnlyList<string> Extensions);
