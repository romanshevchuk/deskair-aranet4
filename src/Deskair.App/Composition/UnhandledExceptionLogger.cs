using Microsoft.Extensions.Logging;

namespace Deskair.App.Composition;

/// <summary>
/// Writes exceptions that nothing caught to the log, so a crash on a user's PC leaves a trace. It does not keep the app alive:
/// after a failure nobody planned for, the app's state cannot be trusted, so it still closes as before.
/// </summary>
public sealed class UnhandledExceptionLogger
{
    private readonly ILogger<UnhandledExceptionLogger> logger;

    public UnhandledExceptionLogger(ILogger<UnhandledExceptionLogger> logger)
    {
        this.logger = logger;
    }

    /// <summary>Listens for the whole life of the process. An exception on the UI thread that nothing handles reaches <see cref="AppDomain.UnhandledException"/> too.</summary>
    public void Attach()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public void OnUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        if (logger.IsEnabled(LogLevel.Critical))
        {
            logger.LogCritical(args.ExceptionObject as Exception, "Deskair stopped because of an error nothing handled (closing: {Terminating}).", args.IsTerminating);
        }
    }

    /// <summary>A background task failed and nobody looked at its result. The app goes on; the failure is only logged.</summary>
    public void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(args.Exception, "A background task failed and nothing handled the error.");
        }

        args.SetObserved();
    }
}
