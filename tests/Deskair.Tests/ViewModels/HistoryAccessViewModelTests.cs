using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Shell;
using Deskair.Services.Messages;
using Deskair.Services.Storage;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public sealed class HistoryAccessViewModelTests : IDisposable
{
    private readonly StrongReferenceMessenger messenger = new();
    private readonly HistoryReadMonitor monitor;
    private readonly TempKnownFolders folders = new();
    private readonly FakeFolderOpener opener = new();
    private readonly HistoryAccessViewModel viewModel;

    public HistoryAccessViewModelTests()
    {
        monitor = new HistoryReadMonitor(messenger);
        viewModel = new HistoryAccessViewModel(monitor, messenger, new FakeUiDispatcher(), new FakeLocalizer(), folders, opener);
    }

    public void Dispose() => folders.Dispose();

    [Fact]
    public void The_banner_is_hidden_while_the_readings_can_be_read()
    {
        Assert.False(viewModel.IsUnreadable);
    }

    [Fact]
    public void The_banner_shows_when_the_readings_cannot_be_read_and_goes_when_they_can_again()
    {
        monitor.ReportFailure();
        Assert.True(viewModel.IsUnreadable);

        monitor.ReportSuccess();
        Assert.False(viewModel.IsUnreadable);
    }

    [Fact]
    public void A_failure_before_the_banner_exists_still_shows_it()
    {
        monitor.ReportFailure();

        var late = new HistoryAccessViewModel(monitor, messenger, new FakeUiDispatcher(), new FakeLocalizer(), folders, opener);

        Assert.True(late.IsUnreadable);
    }

    [Fact]
    public void The_button_opens_the_data_folder()
    {
        viewModel.OpenFolderCommand.Execute(null);

        Assert.Equal([folders.DataFolder], opener.Opened);
    }

    [Fact]
    public void The_texts_are_looked_up_in_the_resources()
    {
        Assert.Equal("Banner_HistoryUnreadable", viewModel.Message);
        Assert.Equal("Banner_HistoryOpenFolder", viewModel.ActionText);
    }
}
