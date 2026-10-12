using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

/// <summary>Runs posted work straight away, so a test sees the result without a UI thread.</summary>
public sealed class FakeUiDispatcher : IUiDispatcher
{
    public int Posted { get; private set; }

    public void Post(Action action)
    {
        Posted++;
        action();
    }

    public Task InvokeAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}
