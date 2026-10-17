# Behaviour rules

*Last reviewed: 2026-10-17*

## CO₂ zones

| Zone | Range |
|---|---|
| Good | below 1,000 ppm |
| Elevated | 1,000 to 1,400 ppm |
| High | above 1,400 ppm |

Thresholds are fixed in v1. Comfort ranges: temperature 20 to 24 °C, humidity 30 to 50 %. Pressure has none.

## Number and time format (D-0020)

Docs may write "1,000"; the app follows this table.

| What | Rule | English | Ukrainian |
|---|---|---|---|
| ppm | Whole number, no group separator, everywhere in the UI (values, axes, labels, toasts, tray tooltip) | 1151 ppm | 1151 ppm |
| Percent | Whole number | 59% | 59 % (narrow no-break space, U+202F) |
| Temperature, humidity, pressure | One decimal for °C and hPa, none for %; decimal mark of the language; no group separator either (pressure is often above 1000) | 21.1 °C, 1013.2 hPa | 21,1 °C, 1013,2 hPa |
| Time | 24-hour, two-digit hour | 16:00 | 16:00 |
| Time range | En dash, no spaces | 16:00–19:00 | 16:00–19:00 |
| Axis hour labels | Full time | 18:00 | 18:00 |

CSV exports are unchanged. Display values use proportional figures (design-tokens.md "Typography").

## Sync and data

1. Sync on launch and on every reconnect. Read the sensor's stored history (up to about 7 days) and merge it by timestamp.
2. Existing readings are never deleted or changed. The first reading stored for a slot stays; a later reading for the same slot, from the sensor or from an import, is ignored (D-0013).
3. Missing readings stay missing. Never interpolate in History.
4. After a sync, Live shows the last 24 h.

## History periods (D-0019)

| Range | Period | ‹ steps to | Title |
|---|---|---|---|
| Day | The last 24 hours, ending now | Yesterday as a calendar day (00:00 to 24:00 local), then the day before | "Last 24 hours", then "Tue 13 Oct" |
| Week | 7 local days ending today | The 7 days before | "8 – 14 Oct 2026" ("29 Sep – 5 Oct 2026" across months) |
| Month | 30 local days ending today | The 30 days before | "15 Sep – 14 Oct 2026" |
| Year | 12 calendar months ending with the current month | Not available in v1 (one year back at most) | "Nov 2025 – Oct 2026" |

- › is off at the latest period. ‹ goes back until the period before the one holding the first reading; that period shows the empty state ("No readings for …", "Go to the latest").
- Changing the range returns to the latest period. Alt+← and Alt+→ do the same as ‹ and ›.

## Aggregation

1. Averages use only existing readings.
2. Calendar days and months use the user's local time. Today and the current month are counted only up to now, so they are not "partial" just because they are not over.
3. Comparison with the previous period uses the same length: for the last 24 hours, the 24 hours before; otherwise the period ‹ would show (`HistoryPeriod.Before`). If the previous period has under 50 % of its expected readings, say "No earlier data to compare".
4. **Grid squares:** an hour square (Week, Month) shows the mean of the readings in that hour, or "no data" if there are none. A day square (Year) shows the day's mean if at least 6 of its hours have readings, else "no data". Hours after now are not drawn. Two or more consecutive days without readings collapse into one row, "No readings · 15 Sep – 5 Oct"; a single empty day stays as empty squares. Nothing is interpolated.
5. **Row end:** each row shows its average, its time in each zone, and a tag on the best (lowest average) and worst (highest average) row, among rows with readings in at least 12 hours (days) or 240 hours (months, ten days' worth), and only when at least 3 rows qualify.
6. **Coverage** is shown as one line under the average, "Based on N % of expected readings", and hidden at 98 % or more with no empty days.
7. **Typical day:** the mean per hour of day over the shown period; for Day, over the 7 days before the shown day; for Year, over the whole year. Week and Month show it as the grid's last row, with the worst three-hour window outlined. Day shows it as a grey line.
8. **Nights:** the night hours from Settings → Sleep (22:00 to 07:00 by default; any start and end; the night may cross midnight; the start must differ from the end). A night belongs to the evening it starts on and is labelled "12–13 Oct". A night counts if at least 6 of its hours have readings. History shows the night hours as a band (grid header, Day chart) and in the summary: the average of the nights, the best and the worst night (Week, Month, Year), or last night (Day; see "Sleep"). There is no Night filter (D-0019) and no Work filter in v1.
9. **Worst-hours tip:** names the worst three-hour window of the typical day and its average. If at least 2 of its 3 hours are night hours, it says "…, while you sleep" and suggests leaving a window slightly open at night; otherwise it suggests opening a window shortly before.
10. **Comfort scales (Temperature, Humidity; POL-18):** the comfortable range is 20 to 24 °C and 30 to 50 % relative humidity, fixed in v1 and held in °C. Steps: Temperature below 18, 18 to 20, 20 to 24, 24 to 26, 26 and above; Humidity below 25, 25 to 30, 30 to 50, 50 to 60, 60 and above. The comfortable range includes both of its limits (24.0 °C and 50 % are comfortable); a value exactly on any other limit belongs to the step nearer the range. `ComfortScale` is the only place that knows the limits; `MetricDescriptor` takes its ideal range from it. The grid colours squares by step; the row end shows the average and the share of readings below, inside and above the range; the tags go to the rows with the most and the least time inside the range, under the same row and count rules as rule 5.
11. **Furthest outside the range:** in the typical row, the outlined three hours are the consecutive three-hour window (00 to 21) whose mean is furthest outside the comfortable range, named by its side: "Coolest" or "Warmest" (Temperature), "Driest" or "Most humid" (Humidity). When every window is inside the range nothing is outlined.
12. **Pressure (POL-18):** no grid, no typical day and no night section. Pressure follows the weather, not the room or the hour, so History shows one line.

## Semantics

- Timestamps are UTC; calendar days and months use the user's local time zone (a day is 23 or 25 hours at a daylight-saving change).
- Buckets are half-open: start inclusive, end exclusive.
- An average is the mean of the existing readings, each weighted equally.
- Coverage is readings present divided by expected slots.

## Live values

- **Trend:** compare the average of the last 6 minutes with the average of the 6 minute window that ends 30 minutes before now. If either window has no readings, the trend is "Steady". A change under 30 ppm is "Steady"; otherwise "Rising" or "Falling" with the ppm change over 30 min. Temperature, humidity and pressure are "Steady" unless they change by 0.5 °C, 3 % or 1 hPa over the last hour, measured from the oldest to the newest reading in that hour. If those readings cover less than 30 minutes (for example just after start-up), the trend is "Steady". Tune with real data.
- "Good in last 24 h": share of time under 1,000 ppm. "Peak": the maximum in 24 h. "Elevated so far" (and Good, High): time since the zone last changed. The Peak label shows the clock time only, also when the peak was yesterday (owner decision 2026-10-17: keep, so the three stats share one 24 h window).
- **Advice and mini cards:** no estimate of how long the air will take to improve (D-0015). The Elevated advice may suggest a duration for the action ("Open a window for about 10 minutes"). The hero advice text follows the zone and the CO₂ trend. A mini card shows the metric's trend. The label judges the value as the card shows it (19.96 °C is shown as 20.0 and is comfortable). Temperature replaces it with "Too cool" or "Too warm" and the ideal range when the value is outside 20 to 24 °C. Humidity replaces it with "A bit dry" (25 to 29 %), "A bit humid" (51 to 55 %), "Too dry" (below 25 %) or "Too humid" (above 55 %), with the ideal range. Pressure never shows a comfort label.
- **Live chart axis:** fitted to the shown readings, padded by 30 ppm and rounded out to steps of 100 ppm (200 ppm when the span is over 300), at least 200 ppm tall. Other metrics keep their current axes.

## Connection states

| State | Trigger |
|---|---|
| Stale | No reading for about 5 minutes (3 or more intervals) |
| Not connected | No reading for 10 minutes (or 5 intervals, if that is longer), or Bluetooth is off |
| Low battery | Sensor at 10 % or below (the footer icon turns amber at 20 %) |
| First run | No paired sensor |

When several apply, the first of this list wins: First run, Not connected, Stale, Low battery. A stale sensor therefore hides the low battery banner; the banner shows again once readings arrive. Stale starts after 3 missed intervals (about 5 minutes at the usual 2 minute interval). Not connected waits for 5 intervals when the sensor is slower than the usual interval, so a sensor that reports every 5 or 10 minutes can still show Stale and does not flip to Not connected just before its next reading.

## Alerts

- Notify on entering Elevated or High after a 5 minute dwell.
- One toast per episode. Notify again only on escalation to High, or if still High after 60 minutes.
- The dwell counts from the start of the episode (first reading outside Good). After a toast at Elevated, escalation to High needs 5 minutes in High before the second toast, so a value hovering at the boundary does not repeat it. Each device has its own episode. A reading at Good ends the episode.
- A toast that is held back (alerts off, Notify at Elevated or High off, quiet hours, Snooze, Do Not Disturb) is not stored. If the air is still Elevated or High when the hold ends, the toast is sent then; if it became Good meanwhile, there is none.
- Quiet hours use the local time. Equal start and end mean no quiet hours.
- Respect quiet hours, Snooze, and Windows Do Not Disturb. Never override Do Not Disturb. Show "Alerts paused by Windows" in the tray tooltip and in Settings.
- The tray icon colour always updates, even when toasts are off.

## Sleep (last night)

1. **Where:** History with the Day range shows "Last night" as a section of the summary card (it replaces the sleep card, D-0019). The chart above shows the day as usual.
2. **Which night:** for the last 24 hours, the most recent night that has ended, in the night hours from Settings → Sleep (22:00 to 07:00 by default). The night in progress is not "last night": at 23:00 the section still shows the night that ended that morning. For a calendar day, the night that ended on that day's morning. Times are local, and a night with a clock change is an hour longer or shorter.
3. **Verdict (CO₂ only):** Good = average under 1,000 ppm and at most 1 hour at or above 1,000 ppm; Poor = average above 1,400 ppm or 5 hours or more at or above 1,000 ppm; otherwise Fair. A reading counts for one sensor interval.
4. **Numbers:** time at or above 1,000 ppm (out of the hours recorded), the peak with its time, and the average between 02:00 and 05:00 local (deep sleep; shown as "–" when the night has none). Other metrics have no verdict and show the lowest value first.
5. **Missing data:** under 80 % of the night recorded shows "Only N of M hours were recorded". A night without readings says so. Nothing is interpolated.
6. **Not built yet:** the morning toast (SLEEP-01).

## Air improved (markers)

- **Detection:** CO₂ at or above 1,000 and a drop of 150 ppm within 10 minutes starts an "air improved" event. It ends when below 1,000, when it flattens (under 10 ppm per 5 min), or after 45 minutes. Call it "air improved", not "window opened". Built as `AirImprovedDetector` (Domain): the event runs from its start through the reading that reached the 150 ppm drop, then on until the first reading that is below 1,000, falls by under 10 ppm per 5 minutes, or is more than 45 minutes after the start; the drop and the duration in the hover are measured to that last reading. A later event starts only after the previous one has ended.
- **Markers (POL-13):** the History Day chart marks the start of each event with a small ring labelled "air improved"; markers are at least 60 minutes apart (a later start inside that hour is not marked). Hover gives the time and the drop ("Air improved at 14:16 · −934 ppm in 12 min").
- **Later (AIR-01):** feedback toasts, markers on the Live ribbon and a typical time to clear per room.

## Later features (documented only)

- **Drill-down:** a row or square in the History grid opens that day or week; the period navigator (D-0019) makes it a change of range and offset.
