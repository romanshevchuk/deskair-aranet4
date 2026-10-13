using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskair.App.Localization;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Platform;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Data;

/// <summary>Saves every stored reading of the selected device to a CSV file the user picks.</summary>
public sealed partial class ExportViewModel : ObservableObject
{
    private readonly ExportService export;
    private readonly DeviceRegistry devices;
    private readonly IFileDialogs dialogs;
    private readonly ILocalizer localizer;
    private readonly LocalCalendar calendar;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ExportViewModel> logger;

    /// <summary>The outcome of the last attempt, or null.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string? statusText;

    public ExportViewModel(
        ExportService export,
        DeviceRegistry devices,
        IFileDialogs dialogs,
        ILocalizer localizer,
        LocalCalendar calendar,
        TimeProvider timeProvider,
        ILogger<ExportViewModel> logger)
    {
        this.export = export;
        this.devices = devices;
        this.dialogs = dialogs;
        this.localizer = localizer;
        this.calendar = calendar;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public bool HasStatus => StatusText is not null;

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (devices.Selected is not { } device)
        {
            StatusText = localizer.Get("Data_NoSensor");
            return;
        }

        var filter = new FileTypeFilter(localizer.Get("Export_FileType"), [".csv"]);
        var path = await dialogs.PickFileToSaveAsync(localizer.Get("Export_DialogTitle"), SuggestedName(device), filter, CancellationToken.None);

        if (path is null)
        {
            return;
        }

        try
        {
            await Task.Run(() => WriteAsync(device, path));
            StatusText = localizer.Format("Export_Done", ("file", Path.GetFileName(path)));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Exporting to {Path} failed.", path);
            }

            StatusText = localizer.Get("Export_Failed");
        }
    }

    private async Task WriteAsync(DeviceId device, string path)
    {
        await using var destination = File.Create(path);

        await export.ExportAsync(device, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, destination, CancellationToken.None);
    }

    private string SuggestedName(DeviceId device)
    {
        var room = devices.Info(device)?.RoomName ?? string.Empty;
        var invalid = Path.GetInvalidFileNameChars();
        var cleanRoom = string.Concat(room.Where(character => !invalid.Contains(character))).Trim();
        var date = calendar.LocalDateOf(timeProvider.GetUtcNow()).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return cleanRoom.Length == 0 ? $"Deskair {date}.csv" : $"Deskair {cleanRoom} {date}.csv";
    }
}
