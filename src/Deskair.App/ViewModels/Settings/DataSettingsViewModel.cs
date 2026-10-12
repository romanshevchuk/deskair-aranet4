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

    // TODO(SET-01g2) [M, haiku]
    // Spec: docs/design/screens-and-states.md "Settings" (Import from phone app, Export CSV)
    // Input : A file chosen through a view-side file dialog service, the device, the export range.
    // Output: Import commands (preview, then import) and an export command that use import and export.
    // Rules : File dialogs go through a view-side service, not System.Windows in the view model.
    // Edge  : Cancelling the dialog does nothing.
    // Done  : Add tests in Tests/ViewModels/DataSettingsViewModelTests with fakes.
    // Needs : questions.md Q28 (file dialog service, device and range for export).
    public Task LoadAsync(CancellationToken cancellationToken)
    {
        DataFolder = folders.DataFolder;

        return Task.CompletedTask;
    }
}
