# Behaviour rules

*Last reviewed: 2026-10*

## CO₂ zones

| Zone | Range |
|---|---|
| Good | below 1,000 ppm |
| Elevated | 1,000 to 1,400 ppm |
| High | above 1,400 ppm |

Thresholds are fixed in v1. Comfort ranges: temperature 20 to 24 °C, humidity 30 to 50 %. Pressure has none.

## Sync and data

1. Sync on launch and on every reconnect. Read the sensor's stored history (up to about 7 days) and merge it by timestamp.
2. Existing readings are never deleted. Sensor values win over imported values.
3. Missing readings stay missing. Never interpolate in History.
4. After a sync, Live shows the last 24 h.

## Aggregation

1. Averages use only existing readings.
2. Calendar days and months use the user's local time. Weeks end today. The current month is marked "so far".
3. Comparison with the previous period uses the same length and filter. If there is no earlier data, say so.
4. A bucket with under 80 % of its expected readings is "partial" (dashed, lighter). A bucket with none is "no data" (hatched).
5. The coverage chip shows "Based on N % of expected readings" and is hidden at 98 % or more with no empty buckets.
6. Typical day: the average per hour of day over the last 7 days (Day, Week), the selected range (Month), or the last 90 days (Year). The worst three-hour window is outlined.
7. Night filter: 22:00 to 07:00. A night belongs to the evening it starts on. There is no Work filter in v1.

## Semantics

- Timestamps are UTC; calendar days and months use the user's local time zone (a day is 23 or 25 hours at a daylight-saving change).
- Buckets are half-open: start inclusive, end exclusive.
- An average is the mean of the existing readings, each weighted equally.
- Coverage is readings present divided by expected slots.

## Live values

- **Trend:** compare the average of the last 6 minutes with the average 30 minutes earlier. A change under 30 ppm is "Steady"; otherwise "Rising" or "Falling" with the ppm change over 30 min. Temperature, humidity and pressure are "Steady" unless they change by 0.5 °C, 3 % or 1 hPa over the last hour. Tune with real data.
- "Good in last 24 h": share of time under 1,000 ppm. "Peak": the maximum in 24 h. "In the X zone": time since the zone last changed.

## Connection states

| State | Trigger |
|---|---|
| Stale | No reading for about 5 minutes (3 or more intervals) |
| Not connected | No reading for 10 minutes, or Bluetooth is off |
| Low battery | Sensor at 10 % or below (the footer icon turns amber at 20 %) |
| First run | No paired sensor |

## Alerts

- Notify on entering Elevated or High after a 5 minute dwell.
- One toast per episode. Notify again only on escalation to High, or if still High after 60 minutes.
- Respect quiet hours, Snooze, and Windows Do Not Disturb. Never override Do Not Disturb. Show "Alerts paused by Windows" in the tray tooltip and in Settings.
- The tray icon colour always updates, even when toasts are off.

## Later features (documented only)

- **Airing detection:** CO₂ at or above 1,000 and a drop of 150 ppm within 10 minutes. Ends when below 1,000, when it flattens (under 10 ppm per 5 min), or after 45 minutes. Call it "air improved", not "window opened".
- **Sleep verdict:** Good = average under 1,000 and at most 1 h above; Poor = average above 1,400 or 5 h or more above; otherwise Fair.
- **Drill-down:** month bar to Month, week bar to Week, day bar to Day; needs a date cursor and a breadcrumb.
