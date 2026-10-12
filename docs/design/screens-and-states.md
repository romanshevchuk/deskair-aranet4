# Screens and states

*Last reviewed: 2026-10*

## Screens

| Screen | Layout |
|---|---|
| Live | Hero (CO₂) left; chart card right with metric tabs, 1h / 6h / 24h, and the 24 h ribbon; three mini cards below |
| History | Toolbar (metric, Hours filter, group by, range, export); main chart; typical-day chart or sleep card; stats card |
| Settings | Two columns: Appearance and units, Alerts; Devices, Data and storage, Start-up |
| Flyout | 360 px: room, value, zone, advice, 24 h ribbon drawn at native size (22 px tall, 12 px hour labels, legend with hours per zone, never a scaled-down copy of the Live ribbon), other readings, Snooze, Open Deskair, Settings |
| Tray menu | Open, status line, Snooze 1 h, Snooze until tomorrow, Show CO₂ on tray icon, Settings, Quit |
| Toasts | Elevated, High (v1); Fresh again, Last night (later) |

Left-click on the tray icon opens the flyout. Right-click opens the menu. Double-click may open the window. The flyout has a filled "Open Deskair" button, closes on click-away or Esc, and closing the main window keeps the tray running. Windows 11 hides new tray icons in the overflow, so first run shows a tip to drag the icon out.

## Live conditions

| Condition | What changes |
|---|---|
| Normal | Full UI |
| Stale data | Banner "Last reading N min ago", hero at 55 % opacity, amber footer dot, Retry |
| Low battery | Banner with Dismiss; footer battery icon red |
| Not connected | Full-screen state with last reading and recovery steps; grey ring; grey tray icon; History stays available |
| First run | Onboarding (steps, nearby sensors, room name and type, import from the phone app); History shows "Collecting your first data" |

Each condition combines with the three zones and both themes. Live has metric (4) and range (3) selection. The ribbon shows for CO₂ on 1h and 6h only.

## History controls

| Control | Options |
|---|---|
| Metric | CO₂, Temperature, Humidity, Pressure |
| Range | Day (last 24 h), Week (7 days), Month (30 days), Year (365 days) |
| Hours | All day, Night (22:00 to 07:00). Day + Night shows last night with the sleep card |
| Group by | Month: by day or by week. Year: by week or by month |

Chart type: hourly line for Day, floating bars otherwise.

| Bucket state | Rule | Rendering |
|---|---|---|
| Complete | 80 % or more of expected readings | Normal bar |
| Partial | Under 80 % | Lighter, dashed outline, hover says "only N of M h recorded" |
| No data | None | Hatched band, merged across neighbours, label with length when wide enough; hover says "device not synced" |

## Tray icon states

Good, Elevated, High: ring in zone colour. Not connected: grey dashed ring with a slash. Quiet hours: zone ring with a moon badge. Value on icon (setting): rounded square with text such as 0.6k, 1.1k, 1.7k; dark text on amber. Tooltip: "Office · 1,068 ppm · Elevated".

## Settings

| Setting | Default | Effect |
|---|---|---|
| Language (System / English / Українська) | System | Whole app, toasts, tray tooltip |
| Theme (Light / Dark / System) | System | Whole app and flyout |
| Temperature unit | °C | Live, History, flyout, toasts (stored data and exports stay in °C) |
| Notify at Elevated / High | On / On | Toasts |
| Quiet hours 22:00 to 07:00 | Off | Mutes toasts only |
| Devices | Rename, Add | Per-device data folder; the selected device is remembered |
| Data folder | Documents\Deskair | Open folder; change location |
| Import from phone app | n/a | Reads the phone's .xlsx export |
| Export CSV | n/a | Writes readings for a chosen range |
| Start with Windows / Keep in tray | On / On | |
| Show CO₂ on tray icon | Off | Value rendering |

Pressure unit and read interval are not app settings (the interval is set in the phone app).
