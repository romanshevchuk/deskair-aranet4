# Review feedback: polish plan

*Last reviewed: 2026-10-19. Source: an outside review of the Live, History and Settings screenshots (2026-10-19), checked against the docs and code. Items the review got wrong are listed at the end so they are not raised again.*

Batch 1 is done (POL-50, D-0022). Batch 2 changes documented behaviour (D-0018, POL-18) and needs an owner decision first. Batch 3 is after release.

## Batch 1: contradictions and colour meaning (done, POL-50)

| ID | Change | Where |
|---|---|---|
| POL-50a | Trend text neutral in every zone (no red "Rising" on a Good card) | `HeroCard.xaml`, `FlyoutView.xaml`; `Trend.*` tokens removed |
| POL-60 | Good + Rising at 800 ppm or more: "Good now, but rising" advice, EN and UK. No time estimate (D-0015) | `AirSummaryBuilder`, `Strings*.resx` |
| POL-50c | Time in zone under 1 min reads "< 1 min" | `HeroStatsBuilder` |
| POL-50d | Header logo is brand ink; only the tray icon follows the zone | `LogoRing`, `HeaderBar.xaml` |
| POL-50e | CO₂ dot neutral slate, so amber means only Elevated | `Metrics.*.xaml` |
| POL-50f | Ideal band on Temperature and Humidity: Good colour at 9 %, dashed limits at 35 %, label top-left, no plot tint | `IdealBandLayer` |

Owner to check: render the gallery and Live in light and dark, and look at the humidity and temperature charts.

## Batch 2: heatmaps (done except the tooltip, D-0023)

Decided 2026-10-19 after a second look. The six CO₂ steps stay: hue gives the zone, lightness the depth, and the legend names Good, Elevated and High. Live stays at three zones, because it answers "do I need to act?" and there are three actions; a "detailed zones" setting would split tests, translations and support for little gain.

| ID | Change | Status |
|---|---|---|
| POL-51 | ~~Three-zone CO₂ grid~~ dropped | dropped |
| POL-52 | Best and worst only on complete rows (not today, not the month in progress): an unfinished day lacks its evening peak and always won "best" | done |
| POL-53 | Caption over the row-end bars: "Time in zones" (CO₂), "Time in range" (Temperature, Humidity) | done |
| POL-54 | Temperature and Humidity: no "best"; "worst" only when that row has time outside the range | done |

Left as they are: the all-green row-end bars on a comfortable Temperature month (revisit with the calendar view, POL-55).

## Batch 3: after release

| ID | Change |
|---|---|
| POL-60 | Floating tooltip beside the grid square (hover and keyboard), with the row and hour labels highlighted; today the readout is a fixed line under the legend. Needs edge handling and keyboard support |
| POL-55 | Month view as a calendar grid (7 columns, about 5 rows, one colour per day, click opens the Day view) |
| POL-56 | Temperature grid on a diverging scale around 22 °C instead of grey for "comfortable" |
| POL-57 | "Typical" line on the Day chart as a quiet band (median and 25th to 75th percentile) with a value in the tooltip |
| POL-58 | Icons in the status badges (✓ / ! / ▲) so zones do not rely on colour alone |
| POL-59 | Temperature and Humidity chart lines coloured by comfort zone, as the CO₂ chart is |

## Not doing, and why

- A "~25 min to High" projection or "you will hit Elevated in 20 min": D-0015 rejected recovery and time estimates because the rate depends on room, doors and people.
- Removing the metric identity colours for Temperature, Humidity and Pressure: the CO₂ clash is fixed by POL-50e; the other three do not collide with a zone colour.
- "No Start with Windows setting": it exists (Settings, Startup). The reviewer saw only the visible part of the page.

## Store listing (owner, P1)

Put "Aranet4" and "CO₂" in the title and description, check Microsoft's rules on third-party product names, and keep "unofficial, not affiliated" prominent. Retake the screenshots without the other page behind the window, in light and dark. Discovery will mostly come from a GitHub release and winget, Aranet communities and a short GIF of the tray flyout.
