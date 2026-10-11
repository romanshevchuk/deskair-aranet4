using System.Windows;
using Deskair.Services.Platform;

namespace Deskair.App.Platform.Threading;

public sealed class WpfUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        Application.Current.Dispatcher.BeginInvoke(action);
    }

    public Task InvokeAsync(Action action)
    {
        return Application.Current.Dispatcher.InvokeAsync(action).Task;
    }
}
