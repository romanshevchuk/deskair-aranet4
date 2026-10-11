# Polish ideas (to make the UI feel more premium)

*Last reviewed: 2026-10*

Ranked by impact for effort. IDs are in `plans/backlog.md` (POLISH-01).

## Highest value, low effort
1. **Real icons.** Replace emoji and symbol characters (⤓ ☾ ⚙ 📈) with Segoe Fluent Icons or custom vectors.
2. **Segoe UI Variable Display for the big numbers** (optical size), with a smaller, lighter unit next to the value.
3. **Sliding selection pill** in segmented controls: the highlight glides to the chosen segment (150 ms).
4. **Soft depth:** two-layer soft shadows, hairline borders with alpha, and a faint inner highlight on dark cards. Hero card gets a very gentle gradient of its tint, not a flat fill.
5. **Pixel snapping:** `UseLayoutRounding`, `SnapsToDevicePixels`, half-pixel aligned 1 px gridlines so lines are crisp at 125 to 200 % DPI.

## Motion (respect the Windows "reduce animations" setting)
6. Number tween when the value changes, gauge marker glide, and a 400 ms cross-fade of the hero tint between zones.
7. Chart line draws in when the tab or range changes; metric tabs cross-fade the chart.
8. Flyout opens with a short fade and 8 px slide, like Windows flyouts; page change fades 150 ms.
9. A small pulsing dot next to "updated 12 s ago" so Live feels alive.

## Data visualisation
10. Smooth lines (monotone cubic), a faded gradient area under the Live line, an end-point dot with a soft glow.
11. Hover tooltip as a rounded card (time, value, zone chip) following the crosshair, instead of text in the header.
12. Rounded bar caps, sparkline end dots, subtle fade of axis labels, softened band edges.

## Windows integration
13. Optional "use Windows accent colour" for focus rings, selected tab and switches.
14. Taskbar overlay badge in the zone colour; acrylic on the flyout; jump list actions (Snooze 1 h, Open History).

## Layout and craft
15. A 4 px grid and concentric radii (inner radius = outer radius − padding).
16. Skeleton placeholders instead of spinners; restore the last view on launch; never shift layout while loading.
17. Per-room colour or icon in the room chip and menu.

## Delight (later)
18. Time-aware microcopy ("Last night was fair"), gentle celebration when air returns to Good, a compact always-on-top mini widget.
