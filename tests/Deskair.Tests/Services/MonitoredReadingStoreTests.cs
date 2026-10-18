using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Messages;
using Deskair.Services.Storage;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Services;

public sealed class MonitoredReadingStoreTests
{
    private static readonly DeviceId device = new("test", "A1");
    private static readonly DateTimeOffset start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeReadingStore inner = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly HistoryReadMonitor monitor;
    private readonly MonitoredReadingStore store;
    private readonly List<bool> changes = [];

    public MonitoredReadingStoreTests()
    {
        monitor = new HistoryReadMonitor(messenger);
        store = new MonitoredReadingStore(inner, monitor, NullLogger<MonitoredReadingStore>.Instance);
        messenger.Register<HistoryReadabilityChanged>(this, (_, message) => changes.Add(message.Unreadable));
    }

    [Fact]
    public async Task A_readable_store_passes_through_and_stays_quiet()
    {
        inner.Readings.Add(new Reading(start, 800, 21, 40, 1010));

        var readings = await store.ReadRangeAsync(device, start, start.AddHours(1), CancellationToken.None);

        Assert.Single(readings);
        Assert.False(monitor.IsUnreadable);
        Assert.Empty(changes);
    }

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    public async Task A_file_that_cannot_be_read_shows_as_nothing_stored_and_is_reported(Type failure)
    {
        inner.FailReads = (Exception)Activator.CreateInstance(failure)!;

        var readings = await store.ReadRangeAsync(device, start, start.AddHours(1), CancellationToken.None);
        var first = await store.GetFirstReadingTimeAsync(device, CancellationToken.None);
        var last = await store.GetLastReadingTimeAsync(device, CancellationToken.None);

        Assert.Empty(readings);
        Assert.Null(first);
        Assert.Null(last);
        Assert.True(monitor.IsUnreadable);
        Assert.Equal([true], changes);
    }

    [Fact]
    public async Task Reading_again_after_the_file_is_free_clears_the_report()
    {
        inner.FailReads = new IOException("locked");
        await store.GetFirstReadingTimeAsync(device, CancellationToken.None);

        inner.FailReads = null;
        await store.GetFirstReadingTimeAsync(device, CancellationToken.None);

        Assert.False(monitor.IsUnreadable);
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public async Task Another_kind_of_error_is_not_hidden()
    {
        inner.FailReads = new InvalidOperationException("bug");

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetFirstReadingTimeAsync(device, CancellationToken.None));
        Assert.False(monitor.IsUnreadable);
    }

    [Fact]
    public async Task A_failed_write_still_reaches_the_caller()
    {
        inner.FailMerges = new IOException("disk full");

        await Assert.ThrowsAsync<IOException>(() => store.MergeAsync(device, [], ReadingSource.Device, TimeSpan.FromMinutes(2), CancellationToken.None));
    }
}
