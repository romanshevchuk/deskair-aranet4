# Components

*Last reviewed: 2026-10*

| Component | Purpose and states | WPF approach |
|---|---|---|
| Window chrome | Logo, nav (Live, History, Settings), room chip, caption buttons | Custom or native title bar; DWM backdrop on Windows 11 |
| Segmented control | One selected option; tabs, ranges, filters, settings | Restyled single-selection `ListBox` (arrow keys move the selection; announced as a selectable list) |
| Toggle switch | 38 × 22 px, knob slides in 0.15 s | ToggleButton template |
| Card | Surface, border, radius 16 | Border style |
| Hero card | Zone tint, pill, value, trend, gauge, advice, three stats | UserControl bound to a zone state |
| Gauge | Three segments, round marker, ticks at 1,000 and 1,400 | Grid with proportional columns |
| Metric mini card | Name, value, subtitle, sparkline; selected has an outline | Button with template |
| Live chart | Zone bands, dashed zone lines, gradient line, crosshair, hover readout | Custom control (DrawingContext) |
| 24 h ribbon | 144 zone-coloured segments, window glyph at airing events | Custom control |
| History chart | Hourly line with band, or floating min–max bars with average tick; weekend shading; hatched gaps; dashed partial bars | Custom control sharing a base with Live chart |
| Typical-day chart | 24 bars by zone, outline on worst 3 h | Custom control |
| Stats card | Average, change, coverage chip, highest/lowest, best/worst period, zone share, insight | Standard panels |
| Banner | Stale data or low battery, with a button | Border row |
| Empty / onboarding card | Icon, title, text, buttons | UserControl |
| Room menu | Rooms with value and zone | Popup on the chip |
| Footer | Status dot, room, sensor ID, signal, battery icon and percent | Grid |
| Toast | Snooze and Dismiss actions | Windows notification toolkit |
| Tray icon | Ring in zone colour, or rounded square with value | Rendered bitmaps at 16, 20, 24, 32 px |
| Flyout | Mini Live view | Borderless window placed from the tray icon rectangle |

All chart controls share one base: axes, bands, hit testing, hover, and a gap hatching brush (tiled rotated lines). The zone-coloured line uses a LinearGradientBrush mapped to y values.
