namespace Deskair.Sensors.Aranet4;

/// <summary>
/// GATT service and characteristic UUIDs and byte layouts. Every value comes from spike S1 (docs/plans/spike-s1-ble-history.md),
/// checked against a real sensor and a phone export on 2026-10-12.
/// </summary>
public static class Aranet4Protocol
{
    /// <summary>The service that holds the sensor characteristics on current firmware (seen on the sensor).</summary>
    public static readonly Guid ServiceId = Guid.Parse("0000fce0-0000-1000-8000-00805f9b34fb");

    /// <summary>The service on older firmware. Not seen on our sensor; kept as a fallback.</summary>
    public static readonly Guid LegacyServiceId = Guid.Parse("f0cd1400-95da-4f4b-9ac8-aa55d312af0c");

    /// <summary>Write a history request here (seen on the sensor).</summary>
    public static readonly Guid CommandId = Guid.Parse("f0cd1402-95da-4f4b-9ac8-aa55d312af0c");

    /// <summary>Number of stored readings, UInt16 little-endian (seen: 5040).</summary>
    public static readonly Guid TotalReadingsId = Guid.Parse("f0cd2001-95da-4f4b-9ac8-aa55d312af0c");

    /// <summary>Seconds between readings, UInt16 little-endian (seen: 120).</summary>
    public static readonly Guid IntervalId = Guid.Parse("f0cd2002-95da-4f4b-9ac8-aa55d312af0c");

    /// <summary>Seconds since the newest reading, UInt16 little-endian.</summary>
    public static readonly Guid SecondsSinceUpdateId = Guid.Parse("f0cd2004-95da-4f4b-9ac8-aa55d312af0c");

    /// <summary>History pages, read repeatedly after a request (the one our sensor used).</summary>
    public static readonly Guid HistoryV2Id = Guid.Parse("f0cd2005-95da-4f4b-9ac8-aa55d312af0c");

    /// <summary>
    /// The current readings in one 13-byte value (seen: <c>6104AF01852633380278003000</c> = 1121 ppm, 21.55 °C, 986.1 hPa, 51 %, battery 56 %, status 2, 120 s, 48 s old).
    /// Layout, little-endian: CO₂ (2), temperature (2), pressure (2), humidity (1), battery % (1), status (1), interval s (2), seconds since measured (2).
    /// </summary>
    public static readonly Guid CurrentReadingsId = Guid.Parse("f0cd3001-95da-4f4b-9ac8-aa55d312af0c");

    public const int CurrentLength = 13;

    public const int CurrentCo2Offset = 0;

    public const int CurrentTemperatureOffset = 2;

    public const int CurrentPressureOffset = 4;

    public const int CurrentHumidityOffset = 6;

    public const int CurrentBatteryOffset = 7;

    public const int CurrentIntervalOffset = 9;

    public const int CurrentAgeOffset = 11;

    /// <summary>The company id in the advertisement's manufacturer data (seen in the scan).</summary>
    public const ushort ManufacturerId = 0x0702;

    /// <summary>
    /// The manufacturer data block of an advertisement, when "Smart Home integration" is on (captured from our sensor on 2026-10-20, 22 bytes, the same layout as the library
    /// Aranet4-Python reads): <c>21070002000901014D04A501902639300278006100AD</c> = 1101 ppm, 21.05 °C, 987.2 hPa, 57 %, battery 48 %, status 2, 120 s, 97 s old.
    /// Byte 0 is flags (bit 5 set: the measurements are included), 1 to 3 the firmware version, 4 to 7 unused here; then the values as in the current-readings value
    /// (CO₂, temperature, pressure, humidity, battery, status, interval, age) from byte 8. A last byte (a counter) follows and is not used.
    /// </summary>
    public const int AdvertisementLength = 21;

    public const int AdvertisementFlagsOffset = 0;

    public const byte AdvertisementIntegrationsFlag = 0x20;

    public const int AdvertisementCo2Offset = 8;

    public const int AdvertisementTemperatureOffset = 10;

    public const int AdvertisementPressureOffset = 12;

    public const int AdvertisementHumidityOffset = 14;

    public const int AdvertisementBatteryOffset = 15;

    public const int AdvertisementIntervalOffset = 17;

    public const int AdvertisementAgeOffset = 19;

    /// <summary>The advertised name starts with this (seen: "Aranet4 0A0B3").</summary>
    public const string NamePrefix = "Aranet4";

    /// <summary>First byte of a history request: <c>61 &lt;metric&gt; &lt;start index, UInt16&gt;</c>.</summary>
    public const byte HistoryRequestCode = 0x61;

    public const byte TemperatureMetric = 1;

    public const byte HumidityMetric = 2;

    public const byte PressureMetric = 3;

    public const byte Co2Metric = 4;

    /// <summary>Page header: metric (1), interval (2), total readings (2), seconds since newest (2), first index (2, 1-based), value count (1).</summary>
    public const int PageHeaderLength = 10;

    /// <summary>Values in one full page (seen: 117 two-byte values, 234 one-byte humidity values, in 244-byte pages).</summary>
    public const int ValuesPerPage = 117;

    public const int HumidityValuesPerPage = 234;

    public const int PageIntervalOffset = 1;

    public const int PageTotalOffset = 3;

    public const int PageAgeOffset = 5;

    public const int PageStartIndexOffset = 7;

    public const int PageCountOffset = 9;

    /// <summary>Temperature: raw / 20 gives °C. Pressure: raw / 10 gives hPa. CO₂ ppm and humidity % are used as they are.</summary>
    public const double TemperatureDivisor = 20.0;

    public const double PressureDivisor = 10.0;

    /// <summary>A raw CO₂ or pressure value with this bit set, or any metric equal to 0, is invalid (none seen in the capture).</summary>
    public const ushort InvalidCo2PressureBit = 0x8000;

    public const ushort InvalidTemperatureBit = 0x4000;

    public const int MaxHumidityPercent = 100;
}
