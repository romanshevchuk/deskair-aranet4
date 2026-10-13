
namespace Deskair.App.ViewModels.Shell;

/// <summary>Shows the main window (it is kept in the tray when closed) and ends the app. Used by the tray icon and its menu.</summary>
public interface IMainWindowController
{
    /// <summary>Brings the main window to the front, opening it if it is hidden, and goes to <paramref name="section"/> when one is given.</summary>
    void Show(ShellSection? section = null);

    /// <summary>Ends the app. Closing the main window does not, because it stays in the tray.</summary>
    void Quit();
}
