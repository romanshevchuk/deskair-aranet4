# Components

*Last reviewed: 2026-10-14*

| Component | Purpose and states | WPF approach |
|---|---|---|
| Window chrome | Logo, text nav (Live, History, Settings) with an accent underline, room chip without border, caption buttons | Custom or native title bar; DWM backdrop on Windows 11. The nav is `NavTabs` (a `ListBox`: arrow keys move the selection) |
| Segmented control | One selected option; ranges, metric (History), settings | Restyled single-selection `ListBox` (arrow keys move the selection; announced as a selectable list); the highlight is one indicator that slides to the chosen segment (`SlidingSelector`, `Motion.Selection`) |
| Toggle switch | 38 × 22 px, knob slides in 0.15 s (not while animations are off); on = `accent` | ToggleButton template, knob animated in code through `MotionPolicy` |
| Card | Surface, 1 px `card.border`, radius 16, neutral shadow | Border style |
| Hero card | Zone glow, pill, value, trend, gauge, advice, three stats; chooses CO₂ for the Live chart | UserControl bound to a zone state, inside a `SelectableCard` |
| Gauge | Three segments, round white marker with an ink ring that glides to a new value, ticks at 1000 and 1400 | Grid with proportional columns; the marker follows an animated `ShownFraction` |
| Selectable card | A card that is one choice in a group (hero and mini cards): runs its command, carries `IsSelected`, reports the selection item pattern to Narrator | `Button` with a bare template and its own automation peer |
| Metric mini card | Name, value, subtitle, sparkline; chooses its metric for the Live chart; chosen has an `accent` ring | `SelectableCard` with a template |
| Live chart | Fitted axis, threshold lines with labels when in range, area fill, monotone-smoothed zone-coloured line, crosshair, hover readout | Custom control (DrawingContext) |
| Sparkline | Smoothed line, area fill, end dot; minimum visible range per metric (1 °C, 6 % RH, 2 hPa) | Custom control: `Sparkline` with `AreaLayer`, `LineLayer`, `EndDotLayer`; the range comes from `SparklineRange` |
| 24 h ribbon | 144 zone-coloured segments; hour labels at true positions; window glyph at airing events (later, AIR-01) | Custom control |
| History chart (Day) | Zone-coloured line with area fill, typical-day line, night band, threshold lines, "air improved" markers, crosshair with the typical value | Custom control sharing a base with Live chart |
| Hour grid chart | Rows of squares in the heat scale: days × hours (Week, Month) or months × days (Year); row average, zone bar, best / worst tag; typical row with the worst three hours outlined; collapsed runs of missing days; night bracket; legend; hover and keyboard focus per square | Custom control on `ChartBase`, one `DrawingVisual`; hit testing by row and column arithmetic |
| Period navigator | ‹ title › for the History period; › off at the latest, ‹ off before the first reading; Alt+← / Alt+→ | Two icon buttons and a text block bound to the History view model |
| Summary card | Average with zone pill and change, coverage line, time in each zone, Nights (or Last night with the sleep verdict), highest and lowest, worst-hours tip | Standard panels; replaces the stats card and the sleep card |
| Banner | Stale data or low battery, with a button | Border row |
| Empty / onboarding card | Icon, title, text, buttons | UserControl |
| Room menu | Rooms with value and zone | Popup on the chip |
| Footer | Status dot, connection and age; sensor model, battery icon and percent (hardware ID in the tooltip of the model) | Grid |
| Logo ring | The logo and tray ring in the zone colour (grey without a zone) | Viewbox with two ellipses |
| Toast | Snooze and Dismiss actions | Windows toast (`Windows.UI.Notifications`, no package) |
| Tray icon | Ring in zone colour, or rounded square with value | Rendered bitmaps at 16, 20, 24, 32 px |
| Flyout | One surface with a zone glow, the reading, ribbon, readings and a footer band | Borderless window placed from the tray icon rectangle. Parts: `ZoneIcon` (advice line), `ZoneRibbon` with `SegmentGap` and `SegmentRadius`, `FractionPanel` (ribbon labels at their true times), `QuietButton` (Snooze), `IconButton` (Settings), a compact `PrimaryButton` |

All chart controls share one base: axes, hit testing, hover. The zone-coloured line uses a LinearGradientBrush mapped to y values.

**Removed (POL items):** metric tabs (POL-06), Typical-day chart (POL-11), Stats card and Sleep card as separate cards (POL-14, now `SummaryCard`), range bars, weekend shading and gap hatching in History (POL-11), zone bands on the Live chart (POL-04).
