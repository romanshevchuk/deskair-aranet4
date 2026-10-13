using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Import;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Sensors;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Data;

/// <summary>
/// Imports a file exported by the sensor's own app into the selected device. The user picks a file and sees a preview;
/// nothing is written until they confirm. The result is a short message, and <see cref="ReadingsImported"/> tells the rest of the app.
/// </summary>
public sealed partial class ImportViewModel : ObservableObject
{
    private readonly ImportService import;
    private readonly ReadingImporters importers;
    private readonly DeviceRegistry devices;
    private readonly IFileDialogs dialogs;
    private readonly ILocalizer localizer;
    private readonly LocalCalendar calendar;
    private readonly IMessenger messenger;
    private readonly ILogger<ImportViewModel> logger;
    private PendingImport? pending;

    /// <summary>What the chosen file holds. Null unless a file is waiting for the user to confirm.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? previewText;

    /// <summary>The outcome of the last attempt, or null.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string? statusText;

    public ImportViewModel(
        ImportService import,
        ReadingImporters importers,
        DeviceRegistry devices,
        IFileDialogs dialogs,
        ILocalizer localizer,
        LocalCalendar calendar,
        IMessenger messenger,
        ILogger<ImportViewModel> logger)
    {
        this.import = import;
        this.importers = importers;
        this.devices = devices;
        this.dialogs = dialogs;
        this.localizer = localizer;
        this.calendar = calendar;
        this.messenger = messenger;
        this.logger = logger;
    }

    public bool HasPreview => PreviewText is not null;

    public bool HasStatus => StatusText is not null;

    [RelayCommand]
    private async Task ChooseFileAsync()
    {
        if (devices.Selected is not { } device)
        {
            Show("Data_NoSensor");
            return;
        }

        if (importers.For(device) is not { } importer)
        {
            Show("Import_Unavailable");
            return;
        }

        var filter = new FileTypeFilter(localizer.Get(importer.DisplayNameKey), importer.FileExtensions);
        var path = await dialogs.PickFileToOpenAsync(localizer.Get("Import_DialogTitle"), filter, CancellationToken.None);

        if (path is null)
        {
            return;
        }

        var request = new PendingImport(path, device, importer, devices.IntervalFor(device, DateTimeOffset.MaxValue));

        try
        {
            var preview = await Task.Run(() => PreviewAsync(request));

            if (preview.TotalReadings == 0)
            {
                Show("Import_Empty");
                return;
            }

            pending = request;
            StatusText = null;
            PreviewText = Describe(preview);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Fail(exception, path);
        }
    }

    [RelayCommand(CanExecute = nameof(HasPreview))]
    private async Task ConfirmAsync()
    {
        if (pending is not { } request)
        {
            return;
        }

        try
        {
            var added = await Task.Run(() => ImportAsync(request));

            Reset();
            StatusText = localizer.Plural("Import_Done", added);
            messenger.Send(new ReadingsImported(request.Device, added));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Fail(exception, request.Path);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Reset();
    }

    private async Task<ImportPreview> PreviewAsync(PendingImport request)
    {
        await using var source = File.OpenRead(request.Path);

        return await import.PreviewAsync(request.Importer, source, request.Device, request.Interval, CancellationToken.None);
    }

    private async Task<int> ImportAsync(PendingImport request)
    {
        await using var source = File.OpenRead(request.Path);

        return await import.ImportAsync(request.Importer, source, request.Device, request.Interval, CancellationToken.None);
    }

    private string Describe(ImportPreview preview)
    {
        return localizer.Plural(
            "Import_Preview",
            preview.TotalReadings,
            ("first", Date(preview.First)),
            ("last", Date(preview.Last)),
            ("new", preview.NewReadings),
            ("present", preview.AlreadyPresent));
    }

    private string Date(DateTimeOffset? instant)
    {
        return instant is { } value ? calendar.LocalDateOf(value).ToString("d MMM yyyy", localizer.Culture) : string.Empty;
    }

    private void Show(string statusKey)
    {
        Reset();
        StatusText = localizer.Get(statusKey);
    }

    private void Fail(Exception exception, string path)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(exception, "Importing {Path} failed.", path);
        }

        Show("Import_Failed");
    }

    private void Reset()
    {
        pending = null;
        PreviewText = null;
        StatusText = null;
    }

    private sealed record PendingImport(string Path, DeviceId Device, IReadingImporter Importer, TimeSpan Interval);
}
