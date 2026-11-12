using System.Text.Json;
using Deskair.Services.Platform;
using Deskair.Services.Settings;

namespace Deskair.Storage.Settings;

/// <summary>Reads and writes window.json in the state folder (%LOCALAPPDATA%\Deskair), so the window's place never travels with the data folder.</summary>
public sealed class JsonWindowPlaceStore : IWindowPlaceStore
{
    private const string FileName = "window.json";

    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IKnownFolders folders;

    public JsonWindowPlaceStore(IKnownFolders folders)
    {
        this.folders = folders;
    }

    private string FilePath => Path.Combine(folders.StateFolder, FileName);

    public async Task<WindowPlace?> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            await using var stream = File.OpenRead(FilePath);
            var place = await JsonSerializer.DeserializeAsync<WindowPlace>(stream, options, cancellationToken);

            return IsUsable(place) ? place : null;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public async Task SaveAsync(WindowPlace place, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folders.StateFolder);

        var temporaryPath = FilePath + ".tmp";

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, place, options, cancellationToken);
            }

            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }

    // A hand-edited or damaged file must not open a window with no size; the window then opens in its default place.
    private static bool IsUsable(WindowPlace? place) => place is { Width: > 0, Height: > 0 };
}
