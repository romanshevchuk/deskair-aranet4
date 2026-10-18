# Sync and import

*Last reviewed: 2026-10-18*

## Sensor sync

- Run on launch and on every reconnect. The current reading is shown as soon as the sensor connects, before the history download, because the first download of a whole week takes about 25 seconds.
- The Aranet4 stores up to about 7 days. The user should open the app at least weekly.
- Sync fills missing slots only (see merge rule in `data-model.md`). Timestamps are computed from the sync time and each reading's age, so allow a few seconds of error and snap to the interval grid.
- A sync starts after the newest stored reading, or at the stored reading before the first hole (a gap of more than one and a half intervals) in the last 7 days, so a skipped slot is filled later (Q51). Merging only fills empty slots, so downloading more than is missing is harmless. A hole the sensor itself never recorded (it was off) cannot be filled and makes each sync download from there.
- Live polling counts from the sensor's own measurement time: the next poll is due one interval after the last value was measured, plus 6 seconds. Counting from the end of the previous poll drifted past the sensor's schedule and, about every few hours, skipped one reading (a hole of one slot in the chart).
- After sync, Live must have the last 24 h.
- The protocol and the comparison with a phone export are in `docs/plans/spike-s1-ble-history.md`.

## Import from the phone app (.xlsx)

The phone export columns are: Time (DD/MM/YYYY H:mm:ss), Carbon dioxide (ppm), Temperature (°C), Relative humidity (%), Atmospheric pressure (hPa), one row every 2 minutes.

```
09/10/2026 12:41:40	987	22.0	51	980.7
09/10/2026 12:43:40	1000	21.9	51	980.8
```

1. Match columns by position; headers may be translated.
2. Parse the date format explicitly. The phone app writes its format in the header (for example `Time(DD/MM/YYYY H:mm:ss)`) and uses the format of its own settings, so read the order from the header. Only when the header is missing or unreadable, ask the user to confirm day-first or month-first. (Today only day-first is read; see questions.md Q7.)
3. Times are local without an offset. Convert with the PC's time zone and flag the repeated hour at the autumn daylight-saving change.
4. Show a preview before importing, for example "64,000 readings, 12 Jul to 9 Oct 2026 · 62,500 new · 1,500 already present".
5. Offer import on first run and in Settings → Data and storage. Also accept CSV or tab-separated text with the same layout.
6. Excel files are read with ExcelDataReader 3.9.0 (MIT; see questions.md Q8). The format is detected by content, not by file name (questions.md Q23). Text and .xlsx share one row parser, so both give the same readings.
7. Because the sensor keeps only about 7 days, import is how older gaps are filled. The coverage chip can link to it.
8. Tests need a real sample file with several thousand rows and a file with a daylight-saving change.
