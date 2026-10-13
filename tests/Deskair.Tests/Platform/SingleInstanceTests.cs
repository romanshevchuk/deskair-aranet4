using Deskair.App.Composition;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Platform;

public sealed class SingleInstanceTests : IDisposable
{
    private readonly string name = "Deskair.Tests." + Guid.NewGuid().ToString("N");
    private readonly List<SingleInstance> instances = [];

    public void Dispose()
    {
        foreach (var instance in instances)
        {
            instance.Dispose();
        }
    }

    private SingleInstance Start()
    {
        var instance = new SingleInstance(name);
        instances.Add(instance);

        return instance;
    }

    [Fact]
    public void The_first_instance_is_first_and_the_second_is_not()
    {
        var first = Start();
        var second = Start();

        Assert.True(first.IsFirst);
        Assert.False(second.IsFirst);
    }

    [Fact]
    public async Task A_second_launch_asks_the_first_to_show_its_window()
    {
        var first = Start();
        var second = Start();
        var shown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.Listen(() => shown.TrySetResult());

        second.SignalFirstInstance();

        await shown.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task A_request_made_while_the_first_instance_was_still_starting_is_not_lost()
    {
        var first = Start();
        var second = Start();
        second.SignalFirstInstance();
        var shown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        first.Listen(() => shown.TrySetResult());

        await shown.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Every_request_is_answered()
    {
        var first = Start();
        var second = Start();
        var count = 0;
        var twice = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.Listen(() =>
        {
            if (Interlocked.Increment(ref count) == 2)
            {
                twice.TrySetResult();
            }
        });

        second.SignalFirstInstance();
        await Wait.Until(() => Volatile.Read(ref count) == 1);
        second.SignalFirstInstance();

        await twice.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Disposing_stops_listening_without_waiting_for_a_request()
    {
        var first = Start();
        first.Listen(() => { });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        first.Dispose();
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void An_instance_that_never_listened_can_be_disposed()
    {
        Start().Dispose();
    }
}
