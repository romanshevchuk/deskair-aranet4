using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Data;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Settings;

/// <summary>
/// Settings → Data and storage: where the data lives, import from the phone app and export to CSV. A new data folder is used from the next start
/// and the files are not copied; the note says so, because moving them is up to the user.
/// </summary>
public sealed partial class DataSettingsViewModel : ObservableObject
{
    private readonly IKnownFolders folders;
    private readonly IFolderOpener folderOpener;
    private readonly IDataFolderChoice choice;
    private readonly IFileDialogs dialogs;
    private readonly ILocalizer localizer;
    private readonly IMessenger messenger;
    private readonly ILogger<DataSettingsViewModel> logger;

    /// <summary>The folder in use now.</summary>
    [ObservableProperty]
    private string? dataFolder;

    /// <summary>"Deskair will use … after it restarts", or a failure message. Null when there is nothing to say.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFolderNote))]
    private string? folderNote;

    public DataSettingsViewModel(
        IKnownFolders folders,
        IFolderOpener folderOpener,
        IDataFolderChoice choice,
        IFileDialogs dialogs,
        ILocalizer localizer,
        ImportViewModel import,
        ExportViewModel export,
        IMessenger messenger,
        ILogger<DataSettingsViewModel> logger)
    {
        this.messenger = messenger;
        this.folders = folders;
        this.folderOpener = folderOpener;
        this.choice = choice;
        this.dialogs = dialogs;
        this.localizer = localizer;
        this.logger = logger;
        Import = import;
        Export = export;
    }

    public ImportViewModel Import { get; }

    public ExportViewModel Export { get; }

    public bool HasFolderNote => FolderNote is not null;

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        DataFolder = folders.DataFolder;
        ShowPendingChange();

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void SyncHistory()
    {
        messenger.Send(new SyncRequested());
    }

    [RelayCommand]
    private void OpenFolder()
    {
        folderOpener.Open(folders.DataFolder);
    }

    [RelayCommand]
    private async Task ChangeFolderAsync()
    {
        var picked = await dialogs.PickFolderAsync(localizer.Get("Settings_Data_ChangeTitle"), CancellationToken.None);

        if (picked is null)
        {
            return;
        }

        try
        {
            choice.Choose(picked);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Choosing {Folder} as the data folder failed.", picked);
            }

            FolderNote = localizer.Get("Data_Folder_Failed");
            return;
        }

        ShowPendingChange();
    }

    private void ShowPendingChange()
    {
        FolderNote = choice.Chosen is { } chosen && !SameFolder(chosen, folders.DataFolder)
            ? localizer.Format("Data_Folder_Restart", ("folder", chosen))
            : null;
    }

    private static bool SameFolder(string first, string second)
    {
        return string.Equals(Path.TrimEndingDirectorySeparator(first), Path.TrimEndingDirectorySeparator(second), StringComparison.OrdinalIgnoreCase);
    }
}
