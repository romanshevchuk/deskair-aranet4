using Deskair.Services.Platform;
using Windows.ApplicationModel;

namespace Deskair.App.Platform.Startup;

/// <summary>
/// Start with Windows for the packaged (Store) build: the startup task the manifest declares (<see cref="TaskId"/>) instead of the Run key.
/// The user can also switch the task off in Windows Settings → Apps → Startup; the app cannot turn it back on then, so enabling fails and the toggle goes back.
/// Reading the state runs on a thread pool thread and is waited for, so it cannot deadlock on the UI thread (it still blocks the caller briefly).
/// </summary>
public sealed class PackagedStartupRegistration : IStartupRegistration
{
    /// <summary>Must match the task id in <c>packaging/AppxManifest.xml</c>.</summary>
    public const string TaskId = "DeskairStartup";

    public bool IsEnabled
    {
        get
        {
            try
            {
                return Task.Run(ReadStateAsync).GetAwaiter().GetResult() is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.Runtime.InteropServices.COMException)
            {
                return false;
            }
        }
    }

    public async Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var task = await StartupTask.GetAsync(TaskId);

        if (!enabled)
        {
            task.Disable();

            return;
        }

        var state = await task.RequestEnableAsync();

        if (state is not (StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy))
        {
            throw new InvalidOperationException($"Windows did not enable the startup task: {state}.");
        }
    }

    private static async Task<StartupTaskState> ReadStateAsync()
    {
        var task = await StartupTask.GetAsync(TaskId);

        return task.State;
    }
}
