namespace Deskair.Services.Sensors;

public enum BleScanMode
{
    /// <summary>Also asks each device for its scan response (the name). Needed to find sensors; makes every sensor in range transmit more.</summary>
    Active,

    /// <summary>Only listens. Enough to follow a sensor that is already known.</summary>
    Passive
}
