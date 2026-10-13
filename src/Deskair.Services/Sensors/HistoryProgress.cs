namespace Deskair.Services.Sensors;

/// <summary>How far a history download is: <see cref="Done"/> of about <see cref="Total"/> pieces (for a Bluetooth sensor, packets) have arrived. The total is an estimate and never less than the done count.</summary>
public readonly record struct HistoryProgress(int Done, int Total);
