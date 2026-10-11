using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Import;
using Deskair.Services.Platform;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class DataSettingsViewModel : ObservableObject
{
    private readonly ImportService import;
    private readonly ExportService export;
    private readonly IKnownFolders folders;

    [ObservableProperty]
    private string? dataFolder;

    public DataSettingsViewModel(ImportService import, ExportService export, IKnownFolders folders)
    {
        this.import = import;
        this.export = export;
        this.folders = folders;
    }

    // TODO(SET-01g) [M, haiku]
    // Spec: docs/design/screens-and-states.md "Settings" (Data folder, Import, Export)
    // Input : A CancellationToken.
    // Output: DataFolder text and the import and export commands (preview then import).
    // Rules : File dialogs go through a view-side service, not System.Windows in the view model.
    // Edge  : Cancelling the dialog does nothing.
    // Done  : Add Tests/ViewModels/DataSettingsViewModelTests with fakes.
    public Task LoadAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-01g)");
}
