namespace Deskair.Tests.Fakes;

public static class Wait
{
    /// <summary>Waits (in real time, up to five seconds) for a background loop to reach a state.</summary>
    public static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was not met within five seconds.");
            }

            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Gives a background loop a moment to do something it should not, before a test asserts that nothing happened. A real-time pause, so it can only miss a late failure, never fail by itself.</summary>
    public static Task Quiet() => Task.Delay(50, TestContext.Current.CancellationToken);
}
