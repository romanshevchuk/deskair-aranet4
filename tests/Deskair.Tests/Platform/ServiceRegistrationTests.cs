using Deskair.App.Composition;
using Deskair.App.ViewModels.Shell;
using Deskair.Services.Connection;
using Deskair.Services.Live;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deskair.Tests.Platform;

/// <summary>A missing registration should fail here, not when the app starts.</summary>
public sealed class ServiceRegistrationTests : IDisposable
{
    private readonly ServiceProvider provider;

    public ServiceRegistrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDeskair(new AppOptions(false, Simulated: true));
        provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = true });
    }

    public void Dispose()
    {
        provider.Dispose();
    }

    [Fact]
    public void The_shell_view_model_has_all_its_parts()
    {
        var shell = provider.GetRequiredService<ShellViewModel>();

        Assert.NotNull(shell.Live);
        Assert.NotNull(shell.Connection);
        Assert.NotNull(shell.Footer);
        Assert.NotNull(shell.Room);
    }

    [Fact]
    public void The_shell_and_its_parts_are_shared_not_created_again()
    {
        var shell = provider.GetRequiredService<ShellViewModel>();

        Assert.Same(shell, provider.GetRequiredService<ShellViewModel>());
        Assert.Same(shell.Live, provider.GetRequiredService<Deskair.App.ViewModels.Live.LiveViewModel>());
        Assert.Same(shell.Footer, provider.GetRequiredService<FooterViewModel>());
    }

    [Fact]
    public void The_services_that_run_in_the_background_resolve()
    {
        Assert.NotNull(provider.GetRequiredService<AppServicesStarter>());
        Assert.NotNull(provider.GetRequiredService<SensorSession>());
        Assert.NotNull(provider.GetRequiredService<ConnectionTracker>());
    }

    [Fact]
    public void The_simulated_app_has_a_radio_that_is_on()
    {
        Assert.True(provider.GetRequiredService<Deskair.Services.Platform.IBluetoothRadio>().IsOn);
    }

    [Fact]
    public void The_simulated_provider_is_registered_for_the_session()
    {
        var providers = provider.GetServices<Deskair.Services.Sensors.ISensorProvider>().Select(candidate => candidate.Id).ToList();

        Assert.Contains("simulated", providers);
    }

    [Fact]
    public void The_simulated_app_stores_its_data_in_the_temp_folder()
    {
        var folders = provider.GetRequiredService<Deskair.Services.Platform.IKnownFolders>();

        Assert.StartsWith(System.IO.Path.GetTempPath(), folders.DataFolder);
    }

    [Fact]
    public void Logging_is_available_to_the_view_models()
    {
        Assert.NotNull(provider.GetRequiredService<ILogger<Deskair.App.ViewModels.Live.LiveViewModel>>());
    }
}
