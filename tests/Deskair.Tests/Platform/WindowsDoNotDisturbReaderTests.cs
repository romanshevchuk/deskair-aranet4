using Deskair.App.Platform.Notifications;

namespace Deskair.Tests.Platform;

public class WindowsDoNotDisturbReaderTests
{
    [Fact]
    public void This_windows_answers_instead_of_failing()
    {
        // Whether Do Not Disturb is on depends on the machine; that Windows answers at all shows the call is shaped right.
        Assert.NotNull(new WindowsDoNotDisturbReader().Read());
    }
}
