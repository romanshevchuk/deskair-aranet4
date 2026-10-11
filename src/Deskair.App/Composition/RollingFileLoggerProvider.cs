using Microsoft.Extensions.Logging;

namespace Deskair.App.Composition;

/// <summary>Writes logs to a rolling file under %LOCALAPPDATA%\Deskair\logs. Not registered yet.</summary>
public sealed class RollingFileLoggerProvider : ILoggerProvider
{
    // TODO(LOG-01a) [M, haiku]
    // Spec: docs/engineering/architecture.md section 11 (Logging); no telemetry.
    // Input : a category name.
    // Output: an ILogger that appends lines to the current log file (time, level, category, message).
    // Rules : one file per day, keep the last 7; the folder comes from IKnownFolders.LogFolder; no packages.
    // Edge  : a failure to write must never throw into the app.
    // Done  : add Tests/App/RollingFileLoggerTests with a temp folder, then register the provider in ServiceRegistration.
    public ILogger CreateLogger(string categoryName)
        => throw new NotImplementedException("TODO(LOG-01a)");

    public void Dispose()
    {
    }
}
