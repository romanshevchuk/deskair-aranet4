using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Storage;

namespace Deskair.App.ViewModels.Shell;

/// <summary>The banner "Can't read your saved readings" (Q27): shown while the saved readings cannot be read, with a button that opens the data folder. Text is already localized.</summary>
public sealed partial class HistoryAccessViewModel : ObservableObject
{
    private readonly IKnownFolders folders;
    private readonly IFolderOpener folderOpener;
    private readonly ILocalizer localizer;

    [ObservableProperty]
    private bool isUnreadable;

    public HistoryAccessViewModel(
        HistoryReadMonitor monitor,
        IMessenger messenger,
        IUiDispatcher dispatcher,
        ILocalizer localizer,
        IKnownFolders folders,
        IFolderOpener folderOpener)
    {
        this.localizer = localizer;
        this.folders = folders;
        this.folderOpener = folderOpener;
        isUnreadable = monitor.IsUnreadable;

        messenger.Register<HistoryAccessViewModel, HistoryReadabilityChanged>(this, (self, message) => dispatcher.Post(() => self.IsUnreadable = message.Unreadable));
        messenger.Register<HistoryAccessViewModel, LanguageChanged>(this, (self, _) => dispatcher.Post(self.RefreshTexts));
    }

    public string Message => localizer.Get("Banner_HistoryUnreadable");

    public string ActionText => localizer.Get("Banner_HistoryOpenFolder");

    [RelayCommand]
    private void OpenFolder()
    {
        folderOpener.Open(folders.DataFolder);
    }

    private void RefreshTexts()
    {
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(ActionText));
    }
}
