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
}
