using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;

namespace Deskair.App.Platform.Startup;

/// <summary>Asks Windows whether the packaged app was started by its startup task (sign-in). The manifest cannot pass an argument, so this is the only sign.</summary>
public static class PackagedStartupActivation
{
    public static bool WasStartedByStartupTask()
    {
        try
        {
            return AppInstance.GetActivatedEventArgs()?.Kind == ActivationKind.StartupTask;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }
}
