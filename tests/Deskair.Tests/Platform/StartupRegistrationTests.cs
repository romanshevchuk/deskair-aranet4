using Deskair.App.Platform.Startup;
using Microsoft.Win32;

namespace Deskair.Tests.Platform;

/// <summary>Uses a throw-away key under HKEY_CURRENT_USER\Software\DeskairTests, never the real Run key.</summary>
public sealed class StartupRegistrationTests : IDisposable
{
    private const string Program = @"C:\Apps\Deskair\Deskair.App.exe";

    private readonly string keyPath = $@"Software\DeskairTests\{Guid.NewGuid():N}";
    private readonly StartupRegistration registration;

    public StartupRegistrationTests()
    {
        registration = new StartupRegistration(keyPath, "Deskair", Program);
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
    }

    [Fact]
    public async Task Nothing_is_registered_to_begin_with()
    {
        Assert.False(await registration.IsEnabledAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Enabling_writes_the_quoted_path_with_the_background_argument_and_reports_enabled()
    {
        await registration.SetEnabledAsync(true, TestContext.Current.CancellationToken);

        Assert.True(await registration.IsEnabledAsync(TestContext.Current.CancellationToken));
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        Assert.Equal($"\"{Program}\" --background", key?.GetValue("Deskair"));
    }

    [Fact]
    public async Task Disabling_removes_the_value()
    {
        await registration.SetEnabledAsync(true, TestContext.Current.CancellationToken);

        await registration.SetEnabledAsync(false, TestContext.Current.CancellationToken);

        Assert.False(await registration.IsEnabledAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Disabling_when_nothing_is_registered_is_fine()
    {
        await registration.SetEnabledAsync(false, TestContext.Current.CancellationToken);

        Assert.False(await registration.IsEnabledAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Enabling_twice_keeps_one_value()
    {
        await registration.SetEnabledAsync(true, TestContext.Current.CancellationToken);
        await registration.SetEnabledAsync(true, TestContext.Current.CancellationToken);

        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        Assert.NotNull(key);
        Assert.Equal(["Deskair"], key.GetValueNames());
    }

    [Fact]
    public async Task A_cancelled_change_leaves_the_state_as_it_was()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => registration.SetEnabledAsync(true, cancelled.Token));

        Assert.False(await registration.IsEnabledAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void The_real_registration_uses_the_per_user_run_key_name()
    {
        Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Run", StartupRegistration.RunKeyPath);
        Assert.Equal("Deskair", StartupRegistration.ValueName);
    }
}
