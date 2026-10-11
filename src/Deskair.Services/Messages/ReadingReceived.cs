using Deskair.Domain.Readings;

namespace Deskair.Services.Messages;

public sealed record ReadingReceived(DeviceId Device, Reading Reading);
