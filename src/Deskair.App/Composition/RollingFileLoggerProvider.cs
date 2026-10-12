using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Composition;

/// <summary>
/// Writes logs to one file per day (<c>deskair-yyyyMMdd.log</c>) in the log folder and keeps the newest seven. Lines: time, level, category, message, then the exception if there is one.
/// A failure to write is swallowed, because logging must never break the app. No telemetry: the files stay on this PC.
/// </summary>
public sealed class RollingFileLoggerProvider : ILoggerProvider
{
    private const int KeptFiles = 7;
    private const string FilePattern = "deskair-*.log";

    private readonly string folder;
    private readonly TimeProvider timeProvider;
    private readonly Lock gate = new();
    private string? currentFile;

    public RollingFileLoggerProvider(string folder, TimeProvider timeProvider)
    {
        this.folder = folder;
        this.timeProvider = timeProvider;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    public void Dispose()
    {
    }

    private void Write(LogLevel level, string category, string message, Exception? exception)
    {
        var now = timeProvider.GetLocalNow();
        var line = new StringBuilder()
            .Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" [").Append(level).Append("] ")
            .Append(category).Append(": ").Append(message);

        if (exception is not null)
        {
            line.AppendLine().Append(exception);
        }

        line.AppendLine();

        try
        {
            lock (gate)
            {
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, string.Create(System.Globalization.CultureInfo.InvariantCulture, $"deskair-{now:yyyyMMdd}.log"));
                File.AppendAllText(path, line.ToString(), new UTF8Encoding(false));

                if (path != currentFile)
                {
                    currentFile = path;
                    DeleteOldFiles();
                }
            }
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            // Deliberately silent: there is nowhere left to report a failure to write the log.
        }
    }

    private void DeleteOldFiles()
    {
        foreach (var old in Directory.GetFiles(folder, FilePattern).OrderByDescending(name => name, StringComparer.Ordinal).Skip(KeptFiles))
        {
            File.Delete(old);
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly RollingFileLoggerProvider provider;
        private readonly string category;

        public FileLogger(RollingFileLoggerProvider provider, string category)
        {
            this.provider = provider;
            this.category = category;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                provider.Write(logLevel, category, formatter(state, exception), exception);
            }
        }
    }
}
