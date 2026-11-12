using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Messages;
using Deskair.Services.Settings;

namespace Deskair.Tests.Services;

public class SettingsServiceTests
{
    private sealed class FakeSettingsStore : ISettingsStore
    {
        private readonly List<string> log;

        public FakeSettingsStore(List<string> log)
        {
            this.log = log;
        }

        public AppSettings Stored { get; set; } = AppSettings.Default;

        public bool FailOnSave { get; set; }

        public int SaveCalls { get; private set; }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(Stored);

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            SaveCalls++;

            if (FailOnSave)
            {
                throw new IOException("disk full");
            }

            Stored = settings;
            log.Add("saved");
            return Task.CompletedTask;
        }
    }

    private readonly List<string> log = [];
    private readonly FakeSettingsStore store;
    private readonly StrongReferenceMessenger messenger = new();
    private readonly SettingsService service;

    public SettingsServiceTests()
    {
        store = new FakeSettingsStore(log);
        service = new SettingsService(store, messenger);
        messenger.Register<SettingsChanged>(this, (_, _) => log.Add("sent"));
    }

    [Fact]
    public async Task Load_reads_the_stored_settings_without_sending_a_message()
    {
        store.Stored = AppSettings.Default with { Theme = ThemeChoice.Dark, AlertsEnabled = false };

        await service.LoadAsync(CancellationToken.None);

        Assert.Equal(ThemeChoice.Dark, service.Current.Theme);
        Assert.False(service.Current.AlertsEnabled);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Load_keeps_the_defaults_when_the_store_returns_defaults()
    {
        await service.LoadAsync(CancellationToken.None);

        Assert.Equal(AppSettings.Default, service.Current);
    }

    [Fact]
    public async Task Update_saves_then_updates_Current_then_sends_the_message()
    {
        await service.UpdateAsync(settings => settings with { Theme = ThemeChoice.Dark }, CancellationToken.None);

        Assert.Equal(ThemeChoice.Dark, store.Stored.Theme);
        Assert.Equal(ThemeChoice.Dark, service.Current.Theme);
        Assert.Equal(new[] { "saved", "sent" }, log);
    }

    [Fact]
    public async Task Update_sends_the_new_settings_in_the_message()
    {
        AppSettings? received = null;
        messenger.Register<SettingsChanged>(new object(), (_, message) => received = message.Settings);

        await service.UpdateAsync(settings => settings with { AlertsEnabled = false }, CancellationToken.None);

        Assert.NotNull(received);
        Assert.False(received.AlertsEnabled);
    }

    [Fact]
    public async Task Update_does_nothing_when_the_change_produces_an_equal_record()
    {
        await service.UpdateAsync(settings => settings with { }, CancellationToken.None);

        Assert.Equal(0, store.SaveCalls);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Update_sends_no_message_and_keeps_Current_when_saving_fails()
    {
        store.FailOnSave = true;

        await Assert.ThrowsAsync<IOException>(
            () => service.UpdateAsync(settings => settings with { Theme = ThemeChoice.Dark }, CancellationToken.None));

        Assert.Equal(AppSettings.Default, service.Current);
        Assert.Empty(log);
    }
}
