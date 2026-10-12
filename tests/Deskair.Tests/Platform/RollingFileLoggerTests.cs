using System.IO;
using Deskair.App.Composition;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace Deskair.Tests.Platform;

public sealed class RollingFileLoggerTests : IDisposable
{
    private readonly TempKnownFolders folders = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2026, 10, 12, 19, 30, 5, 123, TimeSpan.Zero));

    public void Dispose() => folders.Dispose();

    [Fact]
    public void A_line_has_the_time_level_category_and_message()
    {
        using var provider = new RollingFileLoggerProvider(folders.LogFolder, clock);

        provider.CreateLogger("Deskair.Test").LogWarning("Sensor {Address} did not answer", "C3DEC8FCC7C6");

        var text = File.ReadAllText(Path.Combine(folders.LogFolder, "deskair-20261012.log"));
        Assert.Equal("2026-10-12 19:30:05.123 +00:00 [Warning] Deskair.Test: Sensor C3DEC8FCC7C6 did not answer" + Environment.NewLine, text);
    }

    [Fact]
    public void An_exception_follows_its_line()
    {
        using var provider = new RollingFileLoggerProvider(folders.LogFolder, clock);

        provider.CreateLogger("Deskair.Test").LogError(new IOException("Radio is off"), "Connecting failed");

        var text = File.ReadAllText(Path.Combine(folders.LogFolder, "deskair-20261012.log"));
        Assert.Contains("Connecting failed", text, StringComparison.Ordinal);
        Assert.Contains("System.IO.IOException: Radio is off", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Lines_are_appended_to_the_days_file_and_a_new_day_starts_a_new_file()
    {
        using var provider = new RollingFileLoggerProvider(folders.LogFolder, clock);
        var logger = provider.CreateLogger("Deskair.Test");

        logger.LogInformation("first");
        logger.LogInformation("second");
        clock.Advance(TimeSpan.FromDays(1));
        logger.LogInformation("third");

        Assert.Equal(2, File.ReadAllLines(Path.Combine(folders.LogFolder, "deskair-20261012.log")).Length);
        Assert.Single(File.ReadAllLines(Path.Combine(folders.LogFolder, "deskair-20261013.log")));
    }

    [Fact]
    public void Only_the_newest_seven_files_are_kept()
    {
        Directory.CreateDirectory(folders.LogFolder);

        for (var day = 1; day <= 9; day++)
        {
            File.WriteAllText(Path.Combine(folders.LogFolder, $"deskair-202610{day:00}.log"), "old");
        }

        File.WriteAllText(Path.Combine(folders.LogFolder, "notes.txt"), "not a log");
        using var provider = new RollingFileLoggerProvider(folders.LogFolder, clock);

        provider.CreateLogger("Deskair.Test").LogInformation("today");

        var logs = Directory.GetFiles(folders.LogFolder, "deskair-*.log").Select(Path.GetFileName).Order().ToList();
        Assert.Equal(7, logs.Count);
        Assert.Equal("deskair-20261012.log", logs[^1]);
        Assert.Equal("deskair-20261004.log", logs[0]);
        Assert.True(File.Exists(Path.Combine(folders.LogFolder, "notes.txt")));
    }

    [Fact]
    public void A_folder_that_cannot_be_written_never_throws_into_the_app()
    {
        Directory.CreateDirectory(folders.DataFolder);
        var blocker = Path.Combine(folders.DataFolder, "blocked");
        File.WriteAllText(blocker, "this is a file, not a folder");
        using var provider = new RollingFileLoggerProvider(blocker, clock);

        var failure = Record.Exception(() => provider.CreateLogger("Deskair.Test").LogError("nobody will read this"));

        Assert.Null(failure);
    }
}
