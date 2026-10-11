namespace Deskair.Services.Platform;

/// <summary>Lets view models reach the UI thread without using WPF types.</summary>
public interface IUiDispatcher
{
    void Post(Action action);

    Task InvokeAsync(Action action);
}
