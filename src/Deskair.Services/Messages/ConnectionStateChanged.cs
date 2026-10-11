using Deskair.Services.Connection;

namespace Deskair.Services.Messages;

public sealed record ConnectionStateChanged(ConnectionState State);
