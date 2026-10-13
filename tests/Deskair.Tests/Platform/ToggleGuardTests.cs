using Deskair.App.Platform.Tray;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Platform;

public class ToggleGuardTests
{
    private readonly FakeClock clock = new(Samples.Utc(2026, 10, 13, 12));
    private readonly ToggleGuard guard;

    public ToggleGuardTests()
    {
        guard = new ToggleGuard(clock, TimeSpan.FromMilliseconds(400));
    }

    [Fact]
    public void A_flyout_that_was_never_hidden_may_be_shown()
    {
        Assert.True(guard.ShouldShow());
    }

    [Fact]
    public void A_click_right_after_the_flyout_hid_only_closes_it()
    {
        guard.NotifyHidden();
        clock.Advance(TimeSpan.FromMilliseconds(150));

        Assert.False(guard.ShouldShow());
    }

    [Fact]
    public void A_click_a_while_after_the_flyout_hid_opens_it_again()
    {
        guard.NotifyHidden();
        clock.Advance(TimeSpan.FromSeconds(2));

        Assert.True(guard.ShouldShow());
    }
}
