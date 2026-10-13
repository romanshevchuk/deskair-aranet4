namespace Deskair.Domain.Readings;

/// <summary>Good at -60 dBm or stronger, fair down to -75 dBm, weak below. An assumption to tune with real readings (docs/plans/questions.md Q30).</summary>
public static class SignalClassifier
{
    public const int GoodDbm = -60;
    public const int FairDbm = -75;

    public static SignalLevel Classify(int dbm)
        => dbm >= GoodDbm ? SignalLevel.Good : dbm >= FairDbm ? SignalLevel.Fair : SignalLevel.Weak;
}
