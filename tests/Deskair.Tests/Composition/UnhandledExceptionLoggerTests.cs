using System.IO;
using Deskair.App.Composition;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace Deskair.Tests.Composition;

public class UnhandledExceptionLoggerTests
{
    private readonly ListLogger<UnhandledExceptionLogger> logger = new();
    private readonly UnhandledExceptionLogger crashes;

    public UnhandledExceptionLoggerTests()
    {
        crashes = new UnhandledExceptionLogger(logger);
    }

    [Fact]
    public void An_unhandled_exception_is_logged_as_critical_with_the_exception()
    {
        var failure = new InvalidOperationException("boom");

        crashes.OnUnhandledException(null, new UnhandledExceptionEventArgs(failure, isTerminating: true));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Critical, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    [Fact]
    public void A_failed_task_nobody_awaited_is_logged_and_marked_observed()
    {
        var failure = new AggregateException(new IOException("disk"));
        var args = new UnobservedTaskExceptionEventArgs(failure);

        crashes.OnUnobservedTaskException(null, args);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
        Assert.True(args.Observed);
    }
}
