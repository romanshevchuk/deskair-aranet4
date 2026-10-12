using System.Windows.Input;

namespace Deskair.App.Ui.Views.Gallery;

/// <summary>A command that does nothing, so gallery buttons look enabled.</summary>
public sealed class GalleryCommand : ICommand
{
    public static GalleryCommand Instance { get; } = new();

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
    }
}
