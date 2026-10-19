# Screens and states

*Last reviewed: 2026-10-19*

Visual target: `design/polish/deskair-polish-concept.html` (POL-00). Sizes and colours: `design-tokens.md`. Rules: `docs/product/behaviour-rules.md`.

## Screens

| Screen | Layout |
|---|---|
| Live | Hero (CO₂) left; chart card right with its title and summary ("CO₂ · last hour", Low / Avg / High), 1h / 6h / 24h, and the 24 h ribbon; three mini cards below (Temperature, Humidity, Pressure, always in that order). The hero and the mini cards choose the chart's metric; there are no metric tabs |
| History | Toolbar (metric, ‹ period ›, range, export icon); main view (Day: line chart; Week and Month: days × hours grid; Year: months × days grid); summary card on the right at full height |
| Settings | One centred column, at most 720 px: Appearance and units, Alerts, Devices, Data and storage, Sleep, Start-up; section titles above the cards |
| Flyout | 360 px, one surface (no card inside it): see "Flyout" |
| Tray menu | Open, status line, Snooze 1 h, Snooze until tomorrow (and Resume alerts while snoozed), Show CO₂ on tray icon, Settings, Quit |
| Toasts | Elevated, High (v1); Fresh again, Last night (later) |

Left-click on the tray icon opens the flyout. Right-click opens the menu. Double-click may open the window. The flyout has a filled "Open Deskair" button, closes on click-away or Esc, and closing the main window keeps the tray running. Windows 11 hides new tray icons in the overflow, so first run shows a tip to drag the icon out.

## Live

| Part | Behaviour |
|---|---|
| Hero | White card with a zone glow in its top-left corner; "CO₂" and the zone pill (its dot pulses in High while animations are on); the value with "ppm"; the trend; the gauge; the advice (fill, no border); three stats with dividers ("Good in last 24 h", "Peak at 14:16", "Elevated so far", or "< 1 min" in the first minute) |
| Metric choice | The hero chooses CO₂; a mini card chooses its metric. The chosen card has an `accent` ring; the hero shows no ring (CO₂ is the default). Cards never move. Keyboard: the hero and the three mini cards are focusable in reading order (Tab moves between them, Space or Enter chooses); Narrator reads each as a selectable item, "CO₂, 1151 ppm, Elevated, selected". An arrow-key group across the four is not built (follow-up) |
| Chart | Axis fitted to the data (behaviour-rules.md "Live values"); thresholds as labelled lines only when in range; area fill in the same zone colours as the line; monotone smoothing; crosshair and readout as today |
| Ribbon | CO₂ on 1h and 6h only. Hour labels at 00:00, 06:00, 12:00 and 18:00 at their true positions, leaving out any within 2 hours of either end |

## Live conditions

| Condition | What changes |
|---|---|
| Normal | Full UI |
| Connecting | The Live layout with the last values faded at 55 % opacity, a banner "Connecting to your sensor…" (no button), a grey footer dot with the same text, a grey ring and tray icon, the flyout says "Connecting to your Aranet4…". No error screen and no "Try again" until the attempt fails or 45 seconds pass |
| Stale data | Banner "Last reading N min ago", hero at 55 % opacity, amber footer dot, Retry |
| Low battery | Banner with Dismiss; footer battery icon red |
| Not connected | Full-screen state with last reading and recovery steps; grey ring; grey tray icon; History stays available |
| First run | Onboarding (steps, nearby sensors, room name; import from the phone app comes with the import in Settings → Data); History shows "Collecting your first data" |

Each condition combines with the three zones and both themes.

## Flyout

| State | Content, top to bottom |
|---|---|
| Connected | Room name (a grey dot only while not connected or connecting; the zone pill already shows the zone, so a zone-coloured dot would say it twice), "updated 2 min ago". "CO₂" with the zone pill. The value (58 px, as the concept page) with "ppm" and the trend. The advice title as one line with the zone's advice icon. The 24 h ribbon (8 px) with labels at 00:00, 06:00, 12:00 and 18:00 at their true positions (none within 2 hours of the left end or 4 hours of "Now", so the labels never touch) and "Now" at the right end. Temperature, humidity and pressure in three columns (value 16 px 600, unit grey, label below). Footer band: Snooze ▾ (1 h, until tomorrow, Resume; "Snoozed until 14:30" while snoozed) and a Settings icon button on the left, a compact filled "Open Deskair" on the right |
| Not connected | Room with grey dot, "last seen 25 min ago". "Can't reach your Aranet4" and the reason (out of range or asleep, or Bluetooth off). The last reading faded, with its time ("1160 ppm at 12:35"). One recovery step ("Press the button on the Aranet4 to wake it", or "Turn on Bluetooth" when the radio is off; Q48). No ribbon or readings. The same footer band |

The zone glow (grey when not connected) covers the top of the flyout. Clicking the value opens the app, as "Open Deskair" does.

## History controls (D-0019)

| Control | Options |
|---|---|
| Metric | CO₂, Temperature, Humidity, Pressure |
| Period | ‹ and › around a title naming the period ("Last 24 hours", "Tue 13 Oct", "8 – 14 Oct 2026"). Periods and limits: behaviour-rules.md "History periods" |
| Range | Day, Week, Month (30 days), Year (12 months). Changing it returns to the latest period |
| Export | Icon button with the tooltip "Export CSV" |

There is no Hours filter and no Group by. The night hours show as a band and in the summary (behaviour-rules.md "Aggregation" rule 8).

## History views

| Range | Main view |
|---|---|
| Day | Line of the readings, coloured by zone (every metric draws its line as a monotone curve, as on the Live chart; readings the sensor stores coarsely, such as whole-percent humidity, still show as steps: that is the data, POL-32), with a light area fill; the typical day of the 7 days before as a grey line; the night hours as a band labelled "Night"; threshold lines at 1000 and 1400 with coloured axis labels; "air improved" markers; crosshair with the time, the value and the typical value. Legend: CO₂, Typical (previous 7 days), Night |
| Week, Month | Days × hours grid: one row per day (oldest at the top, "Today" last), one square per hour in the heat scale. Above: "Night · 22:00–07:00" bracket and hour labels every 3 hours. Each row ends with its average, a zone bar and a "best" / "worst" tag. Last row: "Typical", with the worst three hours outlined and named ("Worst 17:00–20:00"). Legend at the bottom: the six steps with Good / Elevated / High and their limits, "No data", "Each square is one hour". Over the end column a caption names the bar: "Time in zones" (CO₂) or "Time in range" |
| Year | Months × days grid: one row per month ("Nov 2025" … "Oct 2026"), one square per day (daily average); day numbers 1, 5, 10 … 31 above. A day needs readings in at least 6 different hours for a value, otherwise its square is outlined; days after today are not drawn. Two or more months in a row without readings fold into one row (as in Week). Rows end as in Week. No typical row. Legend as Week, "Each square is one day" |

**Built (POL-13):** the Day line for CO₂ has the area fill, the typical line (hourly means of the 7 days before, laid over the window by clock hour), the night band with its label, dashed limit lines with coloured y labels (the axis tops out at 1600, 2000 or 2600), the "air improved" rings and the legend. The "air improved" markers are a CO₂ rule; Temperature and Humidity add the typical line and the comfort band, and Pressure has neither (POL-18). The average line is gone from Day.

**History per metric (POL-18):** each metric gets the chart that shows its patterns.

| Metric | Week, Month, Year | Day | Summary card |
|---|---|---|---|
| CO₂ | The zones grid, as above | As above | As above |
| Temperature, Humidity | The same grid in the comfort scale (design-tokens.md "Comfort scales"). Row end: average, a below / inside / above bar and a "worst" tag on the row with the least time inside the range (no "best"; D-0023). The typical row outlines the three hours furthest outside the range ("Coolest 03:00–06:00") | Line in the metric colour, the comfort range as a soft band, the typical line and the night band; no area fill | Average with a comfort pill, time in the comfort range, nights, highest and lowest, one tip |
| Pressure | One line (hourly means; daily means for Year), fitted axis, the biggest 24 hour change shaded and labelled | Line, fitted axis, no night band, no typical line | Average, change as neutral text, biggest change in 24 hours, the range, "Pressure follows the weather, not the room" |

**Built (POL-18):** the comfort tokens and rules (design-tokens.md, behaviour-rules.md rules 10 to 12); the Temperature and Humidity hour grid with the comfort share and the "coolest/driest hours" tip; the Day comfort band; the Pressure line with the shaded biggest 24 hour change; and the Pressure summary card (average, change, highest and lowest, biggest change in 24 hours with its start, range of the period, one line about the weather). CO₂ typical day and sleep no longer show on the other metrics.

| Grid state | Rule | Rendering |
|---|---|---|
| Value | Readings in the hour (or 6+ hours in the day) | Square in the heat step |
| No data | No readings in the hour (or under 6 hours in the day) | Empty square with a 1 px outline |
| Missing days | 2 or more consecutive days without readings | One 28 px row, "No readings · 15 Sep – 5 Oct" |
| Future | After now | Not drawn |
| Rows under 14 px | Many rows (Month) | Labels on Mondays and Today only; averages hidden, zone bar and tags kept |

Hover or keyboard focus on a square shows "Tue 13 Oct, 16:00–17:00 · 1234 ppm" ("No data" when empty).

## History summary card

| Section | Content |
|---|---|
| Period card: Average | "Average · 8 – 14 Oct 2026", the value (48 px) with "ppm" and its zone pill; the change as text ("▼ 31 ppm lower than the week before", or "No earlier data to compare"); coverage as one line when shown |
| Time in each zone | The zone bar and the three shares |
| Nights | Week, Month, Year: average of the nights, best and worst night with dates. Day: "Last night" with the verdict pill and the numbers from behaviour-rules.md "Sleep" |
| Extremes | Highest and lowest reading with day and time |
| Tip | The worst-hours tip (behaviour-rules.md "Aggregation" rule 9), in the advice colours of its zone |


**Two cards (2026-10-17):** the summary column is now two cards: the period (average, change, coverage, time in each zone, highest and lowest, the tip) and the night ("Last night" with the verdict pill and its numbers as label and value rows, or the nights of the period). The tip (worst hours, or the comfort advice) sits under the chart, since it is about the hours the chart shows; with it moved, both cards fit at the default window size and at the minimum (1100 × 700), and the column scrolls only for a taller night card or a larger text scale. The Poor night pill is amber and Fair is neutral; red is kept for the air being High now. On the Day chart the typical line is thin and light, the zone colours blend over 30 ppm at each limit so a line that hugs a limit does not flicker, the CO₂ axis is labelled every 200 ppm, and "air improved" is a ring with an entry in the legend instead of a word on each ring.
**Built (POL-14):** `SummaryCard` replaces `StatsCard` and `SleepCard`: average, change, coverage as one line of text ("Based on 82% of expected readings"), time in each zone, "Last night" (Day), highest and lowest with day and time (`Bucket.MaximumAt` and `MinimumAt`), and the tip. Best and worst day are no longer in the card. The tip says "while you sleep" and suggests leaving a window slightly open when at least 2 of the 3 worst hours are night hours. **Built (Q49):** the Nights section for Week, Month and Year (average night; for CO₂ the best and worst night with dates; nights with at least 6 recorded hours). The typical day is a CO₂ rule and is hidden for the other metrics; Pressure has no sleep section.

## History empty states

| State | Main view | Summary |
|---|---|---|
| Before the first reading | "No readings for 24 – 30 Sep", "Deskair has readings from 6 Oct 2026.", "Go to the latest" | The period title and "Nothing was recorded in this period." |
| First run | "Collecting your first data" | Hidden |

## Tray icon states

Good, Elevated, High: ring in zone colour. Not connected: grey dashed ring with a slash. Quiet hours: zone ring with a moon badge. Value on icon (setting): rounded square with text such as 0.6k, 1.1k, 1.7k; dark text on amber. Tooltip: "Office · 1068 ppm · Elevated".

## Settings

| Setting | Default | Effect |
|---|---|---|
| Language (System / English / Українська) | System | Whole app, toasts, tray tooltip |
| Theme (Light / Dark / System) | System | Whole app and flyout |
| Temperature unit | °C | Live, History, flyout, toasts (stored data and exports stay in °C) |
| Notify at Elevated / High | On / On | Toasts |
| Quiet hours 22:00 to 07:00 (one row "Active between [22:00] to [07:00]") | Off | Mutes toasts only |
| Night starts / ends (Sleep) | 22:00 / 07:00 | The night band and the Nights and Last night sections in History; typed as HH:mm, saved when valid; the start must differ from the end |
| Devices | Rename, Add (secondary button) | Per-device data folder; the selected device is remembered |
| Data folder | Documents\Deskair | Open folder; change (used after a restart, files are not copied; Q34) |
| Import from phone app | n/a | Reads the phone's .xlsx export |
| Export CSV | n/a | Writes readings for a chosen range |
| Start with Windows / Keep in tray | On / On | |
| Show CO₂ on tray icon | Off | Value rendering; Settings → Appearance and units, and the tray menu |

Pressure unit and read interval are not app settings (the interval is set in the phone app).
