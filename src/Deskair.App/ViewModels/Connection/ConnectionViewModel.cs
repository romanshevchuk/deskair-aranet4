using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Connection;

namespace Deskair.App.ViewModels.Connection;

public sealed partial class ConnectionViewModel : ObservableObject
{
    [ObservableProperty]
    private ConnectionState state;

    // TODO(STATE-01a) [M, haiku]
    // Spec: docs/product/behaviour-rules.md "Connection states"; docs/design/screens-and-states.md "Live conditions"
    // Input : The new connection state.
    // Output: State plus flags for the stale banner, low battery banner, offline view and first-run view.
    // Rules : Flags are derived from State only.
    // Edge  : Normal clears every flag.
    // Done  : Add Tests/ViewModels/ConnectionViewModelTests.
    public void Update(ConnectionState newState)
        => throw new NotImplementedException("TODO(STATE-01a)");
}
