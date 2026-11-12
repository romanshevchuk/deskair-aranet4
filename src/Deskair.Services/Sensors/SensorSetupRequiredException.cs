namespace Deskair.Services.Sensors;

/// <summary>
/// The sensor was reached but will not share its readings until the user changes one of its own settings. Trying again does not help until then.
/// <see cref="StepKey"/> is the resource key of the sentence that tells the user what to change.
/// </summary>
public sealed class SensorSetupRequiredException : Exception
{
    public SensorSetupRequiredException(string stepKey, string message, Exception innerException)
        : base(message, innerException)
    {
        StepKey = stepKey;
    }

    public string StepKey { get; }
}
