namespace Deskair.Services.Sensors;

/// <summary>The device answered but refused the request (a GATT protocol error), so the connection itself works. Still an <see cref="IOException"/>, like every Bluetooth failure.</summary>
public sealed class BleProtocolException : IOException
{
    public BleProtocolException(string message)
        : base(message)
    {
    }
}
