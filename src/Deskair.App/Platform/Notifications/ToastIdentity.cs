using Microsoft.Win32;

namespace Deskair.App.Platform.Notifications;

/// <summary>
/// The identity an unpackaged app needs to show toasts and to be called back when one is pressed, written for the current user only (no administrator rights):
/// the app identity with its display name and activator, and the COM class whose server is this program. Windows starts the program through that class when a
/// button is pressed while the app is not running. A packaged (Store) build declares the same things in its manifest instead.
/// </summary>
public static class ToastIdentity
{
    public const string AppUserModelId = "Deskair";
    public const string ActivatorClassId = "E4C1D5A2-7B3F-4B8E-9D2A-6F0C8A1B5E37";
    public const string DisplayName = "Deskair";

    /// <summary>Writes the identity. Safe to repeat; it also keeps the program path current after the app moved or was updated.</summary>
    public static void Register(string executablePath)
    {
        using (var identity = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{AppUserModelId}"))
        {
            identity.SetValue("DisplayName", DisplayName);
            identity.SetValue("CustomActivator", $"{{{ActivatorClassId}}}");
        }

        using var server = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{{{ActivatorClassId}}}\LocalServer32");
        server.SetValue(null, $"\"{executablePath}\" -ToastActivated");
    }

    /// <summary>Removes what <see cref="Register"/> wrote.</summary>
    public static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\AppUserModelId\{AppUserModelId}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\CLSID\{{{ActivatorClassId}}}", throwOnMissingSubKey: false);
    }
}
