using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Platform.Power;
using Deskair.Services.Messages;
using Deskair.Tests.Fakes;
using Microsoft.Win32;

namespace Deskair.Tests.Platform;

public sealed class PowerEventsTests : IDisposable
{
    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeClock clock = new(Samples.Utc(2026, 10, 13, 12));
    private readonly PowerEvents power;
    private int resumes;

    public PowerEventsTests()
    {
        power = new PowerEvents(messenger, clock);
        messenger.Register<SystemResumed>(this, (_, _) => resumes++);
    }

    public void Dispose()
    {
        power.Dispose();
    }

    [Fact]
    public void Resuming_from_sleep_is_announced()
    {
        power.Handle(PowerModes.Resume);

        Assert.Equal(1, resumes);
    }

    [Fact]
    public void Going_to_sleep_and_battery_changes_are_not_announced()
    {
        power.Handle(PowerModes.Suspend);
        power.Handle(PowerModes.StatusChange);

        Assert.Equal(0, resumes);
    }

    [Fact]
    public void A_burst_of_resume_events_is_announced_once()
    {
        power.Handle(PowerModes.Resume);
        clock.Advance(TimeSpan.FromSeconds(1));
        power.Handle(PowerModes.Resume);
        clock.Advance(TimeSpan.FromSeconds(2));
        power.Handle(PowerModes.Resume);

        Assert.Equal(1, resumes);
    }

    [Fact]
    public void A_later_resume_is_announced_again()
    {
        power.Handle(PowerModes.Resume);
        clock.Advance(TimeSpan.FromMinutes(30));

        power.Handle(PowerModes.Resume);

        Assert.Equal(2, resumes);
    }

    [Fact]
    public void Disposing_stops_the_announcements()
    {
        power.Dispose();

        power.Handle(PowerModes.Resume);

        Assert.Equal(0, resumes);
    }

    [Fact]
    public void Starting_and_disposing_use_the_real_system_event_without_failing()
    {
        Assert.Null(Record.Exception(() =>
        {
            power.Start();
            power.Dispose();
        }));
    }
}
