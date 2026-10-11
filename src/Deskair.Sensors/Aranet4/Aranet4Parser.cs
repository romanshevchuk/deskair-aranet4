using Deskair.Domain.Readings;

namespace Deskair.Sensors.Aranet4;

/// <summary>Pure parsing: bytes in, readings out, in °C and hPa.</summary>
public static class Aranet4Parser
{
    // TODO(ARANET-02a) [M, sonnet]
    // Spec: docs/plans/spikes.md S1 results (to be written); .claude/rules/sensors.md (parsers are pure).
    // Input : the bytes of the current-readings characteristic, the timestamp to give the reading.
    // Output: a Reading with CO₂ ppm, °C, % and hPa.
    // Rules : apply the scale factors from the spike notes; no I/O.
    // Edge  : too few bytes throws ArgumentException.
    // Done  : unskip Sensors/Aranet4ParserTests.ParseCurrent_* using recorded bytes and make them pass.
    public static Reading ParseCurrent(byte[] bytes, DateTimeOffset timestamp)
        => throw new NotImplementedException("TODO(ARANET-02a)");

    // TODO(ARANET-02b) [L, sonnet]
    // Spec: docs/plans/spikes.md S1 results; docs/engineering/sync-and-import.md "Sensor sync".
    // Input : history packets, the sync instant, the interval and the age of the newest record.
    // Output: readings with timestamps computed from the sync time and each reading's age, snapped to the grid.
    // Rules : allow a few seconds of error; do not invent readings for missing records.
    // Edge  : an empty packet list returns an empty list.
    // Done  : unskip Sensors/Aranet4ParserTests.ParseHistory_* using a recorded capture and make them pass.
    public static IReadOnlyList<Reading> ParseHistory(
        IReadOnlyList<byte[]> packets,
        DateTimeOffset syncedAt,
        TimeSpan interval)
        => throw new NotImplementedException("TODO(ARANET-02b)");
}
