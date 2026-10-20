# Spike S1: Aranet4 history over BLE

*Last reviewed: 2026-10*

Question (from [spikes.md](spikes.md)): can we read the sensor's stored history reliably (about 7 days) and match the phone export?

> **The spike code was removed on 2026-10-13** (last present in commit `07d4f12`, folder `src/Deskair.App/Composition/Spikes/`). The commands in this note no longer exist; the note stays as the record of what was tried and found. What the spike proved is covered by the production code and its tests: `Aranet4Parser`, `Aranet4Sensor` and `SyncService` with the captured download (`tests/Deskair.Tests/TestData/aranet4-history-capture.txt`) and the phone export (`PhoneExportGoldenTests`). A live comparison of a sensor with a phone export on hardware is a manual check; use the git history to bring the tool back.

The spike is `dotnet run --project src/Deskair.App -- --spike-s1`. It is throwaway code in `src/Deskair.App/Composition/Spikes/`. The real code comes with the ARANET and BLE cards after the results are in.

## What it does

1. Scans 12 s for an Aranet4 (name, service or manufacturer id) and uses the strongest one.
2. Pairs if Windows asks for it, finds the history service, and reads the stored count, the interval and the age of the last reading.
3. Downloads the last 7 days (CO₂, temperature, humidity, pressure), with packet counts and timing per metric.
4. Prints the first and last readings, and optionally compares all readings with a phone export.
5. Writes the report, the decoded readings (`readings.csv`) and every raw packet (`packets.txt`) to `artifacts/spike-s1/` (git-ignored), and the report also to the console.

It exits with 0 only when the download is complete.

## Run it (owner)

Bluetooth on, sensor within 1 to 2 m. Run from the repository root. Each run takes about a minute or two.

1. **Phone app closed.** Run:

   ```
   dotnet run --project src/Deskair.App -- --spike-s1 --compare "C:\path\to\phone-export.xlsx"
   ```

   Export from the phone app shortly before the run (same 7 days or more). Leave out `--compare` if you have no export yet.
2. **Phone app open and connected to the sensor.** Run the same command again. Copy `artifacts/spike-s1/` elsewhere first, because each run overwrites it.
3. Optional: **move the sensor far away** (another room) during step 1 to see how a stalled transfer is reported.
4. Paste the contents of each report below, or tell me the file names.

## Exit criteria (from spikes.md)

- [x] Timestamps match the phone export within one interval.
- [x] Sensor interval and capacity documented.
- [x] Behaviour with the phone app connected documented.

**Status: done (2026-10-12).** Open points that do not block the work: a run with the phone app closed, and a run at a weak signal (see "Answer and recommendation").

## Results

### Run 1: phone app closed

Not run yet.

### Run 2: phone app connected (2026-10-12, 21:46)

Sensor at -70 dBm, already paired, phone app connected, week export taken 1.5 minutes earlier.

| Item | Result |
|---|---|
| Service | `0000fce0-0000-1000-8000-00805f9b34fb` found; legacy `f0cd1400-...` not needed |
| Characteristics | `f0cd1401`, `1402`, `1503`, `2001`, `2002`, `2003`, `1502`, `2004`, `3001`, `2005`, `3002` |
| Stored readings / interval | 5040 readings, every 120 s: exactly 7.00 days (the capacity) |
| History characteristic | v2 (`f0cd2005`, read) was used |
| Download | complete, 0 invalid values; CO₂ 6.1 s (44 packets), temperature 5.7 s, humidity 3.0 s (22 packets), pressure 5.9 s; about 21 s in total |
| Phone app connected | no problem: connect and full download worked |
| Export comparison | export has 4973 rows from 2026-10-06 00:01, the sensor from 2026-10-05 21:48, so the first 66 sensor readings (2.2 h) have no partner. The other 4974 all fall within one interval of an export row |

Details of the comparison:

- **The end of the series matches exactly.** Export `21:42:31` = 1071 ppm and `21:44:31` = 1066 ppm; sensor `21:42:30` = 1071 and `21:44:30` = 1066.
- **Export times are not on a perfect 120 s grid.** 4964 gaps are 120 s, but there are 3 of 119 s, 3 of 121 s, one of 107 s and one of 64 s. The seconds drift from `:40` at the start to `:31` at the end. We place readings on a perfect grid backwards from "now minus age", so earlier readings can sit up to about one minute away from the export. That explains the median offset of 51 s.
- **"CO₂ equal at the nearest row: 2152" is not a mismatch.** Pairing by nearest time sometimes picks the neighbouring row when the grids are shifted by about 60 s. The row-by-row check in run 3 settles it.
- **The export starts at 00:01 on 6 Oct** because the owner chose "7 days" in the phone app instead of "All time" (the full history is too large for .xlsx). It is not an error.

### Run 3: row-by-row check and capture (2026-10-12, 22:00)

Phone app state not recorded; sensor at -62 dBm; same export file as run 2. Raw packets and decoded readings were saved.

- Download complete again: 5040 readings, 154 packets (44 + 44 + 22 + 44), 0 invalid values, 20.4 s in total.
- **Row by row, 4973 of 4973 overlapping rows agree on all four values** (export row = sensor row - 59; the shift is 59 because 7 older readings dropped off the sensor and 8 newer ones arrived since run 2). CO₂ is identical in every row; temperature and pressure agree within the export's rounding.
- So the values we decode are the phone app's values, and the phone export's irregular gaps (64 s, 107 s) are only its own timestamps. The values are in the same order.

### Packet layout seen in the capture (input for ARANET-01a and 02a/b)

Every packet is 244 bytes. CO₂, temperature and pressure carry 117 two-byte values; humidity carries 234 one-byte values.

| Bytes | Meaning (first packet: `04 7800 B013 0100 0100 75 ...`) |
|---|---|
| 0 | parameter: 1 temperature, 2 humidity, 3 pressure, 4 CO₂ |
| 1 to 2 | interval in seconds, 0x0078 = 120 |
| 3 to 4 | total stored readings, 0x13B0 = 5040 |
| 5 to 6 | age of the newest reading in seconds |
| 7 to 8 | index (1-based) of the first value in this packet |
| 9 | number of values in this packet (0x75 = 117) |
| 10 to end | the values, little-endian |

Values: CO₂ ppm as is; temperature raw / 20 °C; pressure raw / 10 hPa; humidity % as is. A set high bit (0x8000 for CO₂ and pressure, 0x4000 for temperature) or zero marks an invalid value (none seen). Read protocol: write `61 <parameter> <start index, 2 bytes>` to `f0cd1402`, then read `f0cd2005` repeatedly; each read returns the next page.

The captured files are in `artifacts/spike-s1/` (git-ignored). A trimmed copy should become a test fixture for ARANET-02a/b.

### Answer and recommendation

- Reading the full 7 days over BLE works, takes about 20 s, and was not disturbed by the phone app being connected (run 2).
- Timestamps are within one interval of the phone export for every overlapping reading. The sensor sends no timestamps, only an index; we reconstruct them from the age of the last reading (exact at the end, up to about a minute off at the start of a week).
- All values match the phone export row by row.
- Not shown, and not blocking: behaviour with the phone app closed; behaviour at a weak signal (a stalled transfer). The stall handling in the real client is covered by tests with a fake connection instead.
- Recommendation: build ARANET-01a to 04a and BLE-01a, 02a/b/c on this layout. Check `docs/engineering/sync-and-import.md` for how an incomplete download must be handled before starting those cards.

### Run 4: advertisements without a connection (2026-10-20)

Deskair closed, phone app idle, sensor at -58 to -67 dBm, Smart Home integration on. A throwaway scan (company 0x0702) for 5 minutes in active mode and 3 minutes in passive mode heard the 22-byte block `21070002000901014D04A501902639300278006100AD` (1101 ppm, 21.05 °C, 987.2 hPa, 57 %, battery 48 %, 120 s, 97 s old): about every 3 s (longest gap 14 s), the same measurement for a whole interval, a new one every 120 s, 2 to 3 s behind the sensor; passive mode delivered it too. Layout and decoding: `Aranet4Protocol` (advertisement constants) and `Aranet4AdvertisementTests`. Decision: D-0028.
