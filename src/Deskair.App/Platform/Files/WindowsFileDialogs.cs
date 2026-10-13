using System.Windows;
using Deskair.App.Localization;
using Deskair.Services.Platform;
using Microsoft.Win32;

namespace Deskair.App.Platform.Files;

/// <summary>The built-in Windows open and save dialogs, owned by the main window. Call from the UI thread.</summary>
public sealed class WindowsFileDialogs : IFileDialogs
{
    private readonly ILocalizer localizer;

    public WindowsFileDialogs(ILocalizer localizer)
    {
        this.localizer = localizer;
    }

    public Task<string?> PickFileToOpenAsync(string title, FileTypeFilter filter, CancellationToken cancellationToken)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = Filter(filter, includeAllFiles: true),
            CheckFileExists = true
        };

        return Task.FromResult(Show(dialog));
    }

    public Task<string?> PickFileToSaveAsync(string title, string suggestedName, FileTypeFilter filter, CancellationToken cancellationToken)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = suggestedName,
            Filter = Filter(filter, includeAllFiles: false),
            AddExtension = true,
            OverwritePrompt = true
        };

        return Task.FromResult(Show(dialog));
    }

    public Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken)
    {
        var dialog = new OpenFolderDialog { Title = title };
        var owner = Application.Current?.MainWindow;
        var chosen = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);

        return Task.FromResult(chosen == true ? dialog.FolderName : null);
    }

    private static string? Show(FileDialog dialog)
    {
        var owner = Application.Current?.MainWindow;
        var chosen = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);

        return chosen == true ? dialog.FileName : null;
    }

    /// <summary>
    /// The import reads the file by content (an Excel file or text), so the open dialog also offers all files; the phone app may name its export differently.
    /// </summary>
    private string Filter(FileTypeFilter filter, bool includeAllFiles)
    {
        var patterns = string.Join(';', filter.Extensions.Select(extension => "*" + extension));
        var filters = new List<string> { $"{filter.Description} ({patterns})|{patterns}" };

        if (includeAllFiles)
        {
            filters.Add($"{localizer.Get("Dialog_AllFiles")} (*.*)|*.*");
        }

        return string.Join('|', filters);
    }
}
