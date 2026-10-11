namespace Deskair.Sensors.Aranet4;

/// <summary>GATT service and characteristic UUIDs and byte layouts. Filled in after spike S1 (docs/plans/spikes.md).</summary>
public static class Aranet4Protocol
{
    // TODO(ARANET-01a) [M, sonnet]
    // Spec: docs/plans/spikes.md S1; docs/guides/adding-a-sensor.md.
    // Input : spike S1 results (UUIDs, record layout, history request format).
    // Output: named constants for services, characteristics and offsets used by Aranet4Parser and Aranet4Sensor.
    // Rules : constants only; no logic; document the source of each value in a short comment.
    // Edge  : none.
    // Done  : Aranet4ParserTests reference these constants; build passes.
    public static Guid ServiceId => throw new NotImplementedException("TODO(ARANET-01a)");
}
