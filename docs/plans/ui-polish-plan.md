# UI polish plan

*Last reviewed: 2026-10-14. Source: review of five screenshots (Live CO₂, Live Humidity, History Day, History Month, Settings) plus the theme tokens and the Live XAML; then the flyout and History screenshots.*

Visual concept: [`design/polish/deskair-polish-concept.html`](../../design/polish/deskair-polish-concept.html) shows Live, the flyout and History in all three zones, light and dark. Implementation: [`polish-implementation.md`](polish-implementation.md) (POL-00 to POL-17). Decisions taken in POL-00: D-0017 to D-0021 in `docs/decisions.md`; where this file and the design docs differ, the design docs win.

## Diagnosis

Every element follows the prototype, and every element asks for attention. The amateur feel comes from that, not from any single control:

- **Colour appears two or three times for one meaning.** Elevated shows on the hero background, border, shadow, pill, advice fill, advice border, gauge, chart band, chart line and ribbon. When everything is tinted, the tint stops carrying information.
- **Boxes inside boxes.** Bordered advice card inside a bordered card. Grey segmented tracks inside white cards. Three segmented groups in the History toolbar. Bordered amber callouts stacked inside the stats card.
- **No accent of its own.** Toggles are green (the colour of the Good zone), the primary button is black, focus is blue, and the logo is amber (the colour of Elevated). No colour belongs to the app.
- **Charts look empty.** The Live chart draws 998 to 1151 on a 400 to 2000 axis, so the most interesting data is a flat squiggle. The Month chart spends two thirds of its width on hatching.
- **Composition has holes.** Empty space in the hero between the advice and the stats, under the History stats card, and in the shorter Settings column.

The fix is subtraction. Nothing below adds a feature.

## Top five

| # | Where | Change | Work | Tokens |
|---|---|---|---|---|
| 1 | Hero card, all zones | White card with a soft zone glow in the top-left corner (radial gradient, about 20 % of the zone colour): green in Good, amber in Elevated, red in High. In High the pill's dot pulses (off when Windows animations are off). Remove the zone border and the zone-tinted shadow. Advice card: fill only, no border; the icon loses its white badge. | S | Changes `Zone.*.CardBg`, `Zone.*.CardBorder`, `Shadow.Card.*`, `Zone.*.AdviceBorder`. Needs a design-tokens.md update. |
| 2 | Live chart | Fit the y axis to the visible data with padding, rounded to nice ticks (minimum span per metric, e.g. 200 ppm). Draw a threshold as one dashed line with a label ("Elevated from 1000") only when it is in range. Drop the three bands. Add a vertical area fill under the line (line colour at 22 % fading to 0). Smooth the line with monotone cubic interpolation. | M | Bands (`Chart.Band.*`) stay for History or go. New `Chart.Area.Opacity`. Code: `LiveChart.cs`, `Layers/BandLayer.cs`, `Layers/LineLayer.cs`, `ChartLines.cs`. |
| 3 | Hero value | 96 to 104 px, Light, no thousands separator under 10000 ("1151"), tighter tracking. Use proportional figures on the hero only (`Text.HeroValue` inherits `NumeralAlignment=Tabular` from `Text.Number`, which gives each "1" a full digit width and opens a gap in "1151"); keep tabular figures where numbers line up in columns. WPF has no letter-spacing, so try `Segoe UI Variable Display` Semilight at 104 instead. Apply "no separator" to every ppm value (stats, gauge ticks, chart labels) so the screen is consistent. Decided (D-0020): no separator in either language, for all metric values. | S | `Font.Size.Hero`. Formatting lives in the view models; check `docs/product/behaviour-rules.md` for number format rules. |
| 4 | One accent colour | Pick ink (`Text.Primary`, near-black; near-white in dark) as the app accent: selected nav, toggles on, primary button, selected mini card ring. Zones are the only colour in the app. Toggles stop being green, so green only ever means Good air. | S | `Toggle.On`, `PrimaryButton` background. Keeps `Focus.Ring` blue (accessibility). |
| 5 | Live metric switching | Remove the CO₂ / Temperature / Humidity / Pressure tabs from the chart card. The hero selects CO₂, each mini card selects its metric, and the selected card gets the ink ring. Cards keep their places; today the mini row swaps cards when you switch, which reads as a layout jump. Needs a fourth mini card, or the hero acting as CO₂'s selector. | M | `MetricTabs.xaml` goes; `MiniCardsRow.xaml`, `LiveViewModel`. Check `screens-and-states.md` and the keyboard path in `accessibility.md`. |

## Then

| # | Where | Change | Work | Tokens |
|---|---|---|---|---|
| 6 | Header navigation | Live / History / Settings as text with a short 3 px underline, not white pills. Global navigation should not look like the filters inside cards. Room chip: no border or shadow, hover background only. | S | `HeaderBar.xaml`, `RoomChip.xaml`. |
| 7 | Mini-card sparklines | Minimum visible range per metric (1 °C, 6 % RH, 2 hPa) so steady values draw steady. Today 0.5 hPa of noise draws as tall spikes and humidity is a flat line floating mid-card. Same size and baseline in all three, with an area fill and an end dot. | S | `Sparkline.cs`. |
| 8 | History | Replaced by the History rework below. The earlier items 8 to 11 (quieter line charts, cropped gaps, one callout, a smaller toolbar) are all part of it. | L | See below. |
| 12 | Settings layout | One column, max width about 720 px, centred: the Windows 11 Settings pattern. Two masonry columns of unequal height leave a hole. Section titles at 14 SemiBold above the cards (outside them), not 12 px grey inside. Time fields ("22:00") as compact 88 px inputs, not 200 px boxes. "Add" becomes a secondary button; the only filled button should be the main action of a screen. | M | `SettingsView.xaml`, `SettingsCard`, `Size.Input.MinWidth`. |
| 13 | Footer | Hide the hardware ID (tooltip on the sensor name). Do not repeat the room name already shown in the header chip. Show "Aranet4 · 59%". | S | `FooterBar.xaml`. |
| 14 | Gauge marker | White knob with an ink ring and a small shadow instead of the heavy black ring; 20 px. | S | `Gauge.xaml`, `Size.Gauge.Marker`. |
| 15 | Motion | The biggest "premium" signal the renders cannot show. In order of value: (a) a sliding selection indicator in segmented controls and the nav underline (150 ms ease-out); (b) the hero value cross-fades when it changes; (c) the chart line eases between ranges instead of jumping; (d) a slow pulse on the chart's last point. Honour Windows "Animation effects" off (`SystemParameters.ClientAreaAnimation`). | M | New `Motion.*` durations next to `Motion.Toggle`. |

## History rework

Reviewed from three screenshots (Day, Week, Month + Night) and the screens doc. This is a bigger change than the rest of the plan: it changes the History controls in `screens-and-states.md` and replaces two chart types. The concept page shows all four ranges, both data states ("Since 6 Oct" and a full example year), light and dark, with hover values.

### What is wrong today

- **No way back in time.** Day, Week, Month and Year are rolling windows ending now. Last Tuesday, or the week before last, cannot be opened.
- **Four control groups** (metric, All day / Night, By day / By week, range) plus export. It reads as a control panel, and two of the groups change the chart in ways that are hard to predict.
- **Range bars answer the wrong question.** A bar from the day's minimum to its maximum is set by one spike, so the biggest shape on screen is the least useful number. You can't see when in the day the air was bad.
- **Typical day is a second card** with its own axis, next to a chart that already holds the same data.
- **Gaps dominate.** Month spends two-thirds of its width on hatching; weekend shading on top makes it noisy.
- **The stats card** stacks boxed callouts (coverage, worst hours) and leaves the space below it empty.

### The new History

| # | Change | Work | Notes |
|---|---|---|---|
| H1 | **Period navigator.** ‹ › next to a title that names the period ("8 – 14 Oct 2026", "Tue 13 Oct", "Last 24 hours"). Day k=0 stays the rolling last 24 hours; ‹ goes to calendar days. Week, Month (30 days) and Year step by their length. › is disabled at the latest period; ‹ stops one period before the first reading and shows an empty state with "Go to the latest". | M | `HistoryViewModel` gets a period offset; `ReadingQueryService` already answers arbitrary ranges for export. Keyboard: Alt+← / Alt+→. |
| H2 | **Two control groups.** Metric and range. The Night filter goes: nights are a band in every view and a section in the summary. By day / By week goes: the grid shows days by its shape. Export becomes an icon button. | S | Update the History controls table in `screens-and-states.md` and the Night filter rule in `behaviour-rules.md`. Night hours from Settings still drive the band and the summary. |
| H3 | **Week and Month: days × hours grid.** One row per day, one square per hour, coloured in six CO₂ steps (two per zone: <800, 800–1000, 1000–1200, 1200–1400, 1400–2000, 2000+). Each row ends with its average, a small zone-split bar and a "best" / "worst" tag. Labels on every row when rows are 14 px or taller, else Mondays and Today. | L | New `HourGridChart` in `Ui/Charts`, reusing `ChartBase` and hit testing. Six new `Heat.*` tokens per theme. A grid is a "semantic heat" scale, so it needs its legend (always shown at the bottom). |
| H4 | **Typical row.** The grid's last row is each hour's average over the period, with the worst three hours outlined. It replaces `TypicalDayCard`. | S (with H3) | `TypicalDayChart.cs` and its card go. |
| H5 | **Gaps collapse.** Two or more days without readings become one line ("No readings · 15 Sep – 5 Oct"). A single missing day or hour stays as outlined squares. No hatching, no weekend shading. | S (with H3) | `GapHatchLayer`, `NoDataLayer` and `WeekendShadingLayer` are no longer used by History. |
| H6 | **Year: months × days grid.** Same component, one square per day (daily average, needs at least 6 hours), one row per month, month average and zone split at the end. Shows seasons and the weeks the sensor was off. | M (with H3) | Replaces the Year range bars. |
| H7 | **Day: the line against typical.** Zone-coloured line, threshold hairlines, night shaded, and a grey line for the typical day of the previous 7 days. The start of each "air improved" event (behaviour-rules.md "Air improved") is marked. Hover shows the time, the value and the typical value. | M | `HistoryChart.cs` (Day mode). The marker uses the documented airing detection rule; feedback toasts and ribbon markers stay with AIR-01. |
| H8 | **Summary card, top to bottom.** Average with its zone pill and change vs the previous period (text, no filled pill), coverage as one line when under 90 %, time in each zone, Nights (average, best and worst night; in Day view "Last night": average, peak, time in Good, which absorbs `SleepCard`), highest and lowest with day and time, then one tip. The card spans the full height. | M | `StatsCard.xaml`, `SleepCard.xaml` merged; `CoverageChip` becomes text. Best / worst day move into the grid. |
| H9 | **Tip copy follows the hours.** When the worst three hours fall at night, the tip says so ("…, while you sleep") and suggests a window left slightly open, not "open a window shortly before". | S | New strings in both languages (`add-a-translation`). |

### Other metrics

The concept draws CO₂ only. Temperature, humidity and pressure use the same grid with a single-hue scale per metric (light to dark), and humidity marks its comfort band in the legend. Pressure is the weakest fit for a grid (it changes with weather, not with the room); a Day / Week line is enough there, so the grid can stay CO₂, temperature and humidity.

### Accessibility

Each square and the line have hover values today in the concept; the app needs the same on keyboard focus (arrow keys move between squares) and a list alternative for Narrator ("Tue 13 Oct, 16:00 to 17:00, 1234 ppm, Elevated"). The colour steps are never the only cue: the row average, the tags and the tooltip carry the numbers.

## Tray flyout

Reviewed from one screenshot (Elevated, light). The flyout repeats the Live pattern at a smaller size: a bordered zone card inside a bordered panel, and a full-width black "Open Deskair" bar that is the heaviest thing in it. The concept page shows the flyout in all three zones and in Not connected.

| # | Where | Change | Work | Tokens |
|---|---|---|---|---|
| F1 | Flyout, CO₂ block | Drop the inner `Card`. The zone shows as a glow across the top of the flyout and in the pill; the number, trend and advice sit straight on the flyout surface. Same number treatment as item 3 ("1160", proportional figures). Advice as one line with a zone-coloured icon. | S | `FlyoutView.xaml` (the `controls:Card` goes). Same glow tokens as item 1. |
| F2 | Footer buttons | Put the buttons in a slightly darker band with a top hairline, as in Windows 11 Quick Settings. Snooze becomes a text button with a bell icon and ▾; Settings an icon button; Open Deskair stays filled (screens-and-states.md requires it) but compact and right-aligned instead of stretched. | S | New `Flyout.Band` colour; `Space.Flyout.Footer`. |
| F3 | 24 h ribbon | 8 px high instead of 22, rounded segments. Labels at round hours at their true positions (18:00, 00:00, 06:00) plus "Now". Today they sit at 25 / 50 / 75 % of a window that ends at the current minute, so they read 13:20, 19:20, 01:20, 07:20. Keep the time-in-zone legend. | S | `Size.Flyout.Ribbon.Height`; the label grid in `FlyoutView.xaml` becomes positioned labels, as on Live. screens-and-states.md fixes 22 px, so update it too. |
| F4 | Other readings | Value semibold, unit grey and smaller, thin dividers between the three. "51%" in English (see Small things). | S | `KeyValueCell` gets a unit part, or a flyout-only template. |
| F5 | Not connected | Keep the last reading visible, faded, with its time ("1160 ppm at 12:35"), and one step to try ("Press the button on the Aranet4 to wake it"). The strings already exist: `Offline_LastReading`, `Offline_Step_Wake`. The glow and the tray ring go grey. | S | `FlyoutView.xaml`, `FlyoutViewModel`. |
| F6 | Surface | Match the Windows flyouts next to it: 8 px corners, a lighter shadow, and acrylic if possible. Acrylic needs a non-layered window with `DWMWA_SYSTEMBACKDROP_TYPE` = transient (Windows 11 22H2+). The flyout's shadow margin (`Space.Menu.Shadow`) suggests `AllowsTransparency`, which rules that out, so acrylic is a rework. Do the corners and shadow first. | S (corners, shadow), L (acrylic) | `Radius.Flyout`, `Shadow.Flyout`. |

## Small things

- **Percent spacing is mixed** on one screen: "15%" (stats), "51 %" (mini card), "26 %" (coverage chip, `Coverage_Chip` in `Strings.resx`), "35% Good" (zone share). English: no space. Ukrainian: a space. Put the rule in the string or the formatter, not in each view.
- **Time formats are mixed**: "16–19h", "00h", "16:00–19:00", "18h" on the ribbon. Pick "16:00–19:00" for prose and "18:00" or "18" for axes, and use the same on Live and History.
- **Humidity copy**: "Too humid · ideal 30–50 %" at 51 % reads as an alarm over 1 %. Decided (D-0021): "A bit humid" / "A bit dry" within 5 points of the ideal range, "Too …" beyond.
- **"In the Elevated zone"** (`Stat_InZone_*`) wraps to two lines in the hero's third stat while the other two labels take one. "Elevated so far" fits on one line.
- **Logo colour**: the amber ring is the Elevated colour. If the ring follows the zone (as the tray icon does), say so in design-tokens.md; if it is a brand mark, make it ink.
- **Window background**: in the Settings and History screenshots a peach tint shows in the lower-left corner. That is Mica picking up the wallpaper through a semi-opaque background. Either make `Window.Bg` fully opaque, or commit to Mica and make cards slightly translucent so the effect looks intended.
- **Type scale**: the tokens have 12, 13, 13.5, 14, 15, 16 and 17 px. Seven sizes within 5 px look accidental. Aim for 12 / 13 / 14 / 16 / 20 / 32 / 104.

## What I could not see

Dark theme, Ukrainian, the flyout, first run, empty and offline states, hover and focus, and real DPI scaling. Run the render tests (`DESKAIR_RENDER=1`) and check items 1, 4 and 8 in dark mode first: soft glows and low-opacity fills behave differently on dark surfaces.

## Suggested order

1. Items 1, 3, 4, 6, 13 and the small things: one afternoon, tokens and XAML only, and most of the "premium" gap closes.
2. Items 2 and 7 (charts): the biggest visible change on Live.
3. Item 5, then the History rework (H1, H2 and H8 first: they help even before the grid exists; then H3 to H7), then Settings (12).
4. Motion (15) last, once the static look is settled.
