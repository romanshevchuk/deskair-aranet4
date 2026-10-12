using System.Text.Json;
using System.Text.Json.Serialization;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Services.Platform;
using Deskair.Services.Settings;

namespace Deskair.Storage.Settings;

/// <summary>Reads and writes settings.json in the data folder.</summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private const int FileVersion = 1;
    private const string FileName = "settings.json";

    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IKnownFolders folders;

    public JsonSettingsStore(IKnownFolders folders)
    {
        this.folders = folders;
    }

    private string FilePath => Path.Combine(folders.DataFolder, FileName);

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return AppSettings.Default;
            }

            await using var stream = File.OpenRead(FilePath);
            var file = await JsonSerializer.DeserializeAsync<SettingsFile>(stream, options, cancellationToken);

            return file is null ? AppSettings.Default : file.ApplyTo(AppSettings.Default);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return AppSettings.Default;
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folders.DataFolder);

        var temporaryPath = FilePath + ".tmp";

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, SettingsFile.From(settings), options, cancellationToken);
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

    private sealed class SettingsFile
    {
        public int? Version { get; init; }

        public LanguageChoice? Language { get; init; }

        public ThemeChoice? Theme { get; init; }

        public bool? AlertsEnabled { get; init; }

        public TemperatureUnit? TemperatureUnit { get; init; }

        public DeviceId? SelectedDevice { get; init; }

        public bool? NotifyAtElevated { get; init; }

        public bool? NotifyAtHigh { get; init; }

        public bool? QuietHoursEnabled { get; init; }

        public TimeOnly? QuietHoursStart { get; init; }

        public TimeOnly? QuietHoursEnd { get; init; }

        public static SettingsFile From(AppSettings settings) => new()
        {
            Version = FileVersion,
            Language = settings.Language,
            Theme = settings.Theme,
            AlertsEnabled = settings.AlertsEnabled,
            TemperatureUnit = settings.TemperatureUnit,
            SelectedDevice = settings.SelectedDevice,
            NotifyAtElevated = settings.NotifyAtElevated,
            NotifyAtHigh = settings.NotifyAtHigh,
            QuietHoursEnabled = settings.QuietHoursEnabled,
            QuietHoursStart = settings.QuietHoursStart,
            QuietHoursEnd = settings.QuietHoursEnd
        };

        public AppSettings ApplyTo(AppSettings defaults) => defaults with
        {
            Language = Language ?? defaults.Language,
            Theme = Theme ?? defaults.Theme,
            AlertsEnabled = AlertsEnabled ?? defaults.AlertsEnabled,
            TemperatureUnit = TemperatureUnit ?? defaults.TemperatureUnit,
            SelectedDevice = SelectedDevice ?? defaults.SelectedDevice,
            NotifyAtElevated = NotifyAtElevated ?? defaults.NotifyAtElevated,
            NotifyAtHigh = NotifyAtHigh ?? defaults.NotifyAtHigh,
            QuietHoursEnabled = QuietHoursEnabled ?? defaults.QuietHoursEnabled,
            QuietHoursStart = QuietHoursStart ?? defaults.QuietHoursStart,
            QuietHoursEnd = QuietHoursEnd ?? defaults.QuietHoursEnd
        };
    }
}
