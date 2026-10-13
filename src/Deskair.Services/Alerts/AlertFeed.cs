using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Microsoft.Extensions.Logging;

namespace Deskair.Services.Alerts;

/// <summary>Gives every reading the sensor session announces to the <see cref="AlertEngine"/>, which decides whether it is time for a toast. A failing toast is logged and never stops later readings.</summary>
public sealed class AlertFeed
{
    private readonly IMessenger messenger;
    private readonly DeviceRegistry devices;
    private readonly AlertEngine engine;
    private readonly ILogger<AlertFeed> logger;
    private CancellationToken stopped;

    public AlertFeed(IMessenger messenger, DeviceRegistry devices, AlertEngine engine, ILogger<AlertFeed> logger)
    {
        this.messenger = messenger;
        this.devices = devices;
        this.engine = engine;
        this.logger = logger;
    }

    /// <summary>Starts following readings until <paramref name="cancellationToken"/> is cancelled.</summary>
    public void Start(CancellationToken cancellationToken)
    {
        stopped = cancellationToken;
        messenger.Register<AlertFeed, ReadingReceived>(this, static (self, message) => _ = self.HandleAsync(message));
        cancellationToken.Register(() => messenger.UnregisterAll(this));
    }

    private async Task HandleAsync(ReadingReceived message)
    {
        try
        {
            var room = devices.Info(message.Device)?.RoomName ?? message.Device.Address;

            await engine.OnReadingAsync(message.Device, room, message.Reading, stopped);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Handling a reading for the alerts failed.");
            }
        }
    }
}
