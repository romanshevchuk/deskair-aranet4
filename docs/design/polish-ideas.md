# Polish ideas (to make the UI feel more premium)

*Last reviewed: 2026-10-14*

Ranked by impact for effort. The polish rework (`plans/ui-polish-plan.md`, items POL-00 to POL-17 in `plans/polish-implementation.md`) takes over several of these; the item that builds an idea is noted after it.

## Highest value, low effort
1. **Real icons.** Replace emoji and symbol characters (⤓ ☾ ⚙ 📈) with Segoe Fluent Icons or custom vectors.
2. **Segoe UI Variable Display for the big numbers** (optical size), with a smaller, lighter unit next to the value. *(done, Q47)*
3. **Sliding selection pill** in segmented controls: the highlight glides to the chosen segment (150 ms). *(POL-16)*
4. **Soft depth:** two-layer soft shadows, hairline borders with alpha, and a faint inner highlight on dark cards. Hero card gets a very gentle gradient of its tint, not a flat fill. *(card shadows and borders done, Q47; the hero gets a zone glow instead of a tint, POL-01, D-0017)*
5. **Pixel snapping:** `UseLayoutRounding`, `SnapsToDevicePixels`, half-pixel aligned 1 px gridlines so lines are crisp at 125 to 200 % DPI.

## Motion (respect the Windows "reduce animations" setting)
6. Number tween when the value changes, gauge marker glide, and a 400 ms cross-fade of the hero glow between zones. *(POL-16)*
7. Chart line draws in when the metric or range changes; the chart cross-fades between metrics (the tabs go, POL-06).
8. Flyout opens with a short fade and 8 px slide, like Windows flyouts; page change fades 150 ms. *(POL-16)*
9. A small pulsing dot next to "updated 12 s ago" so Live feels alive.

## Data visualisation
10. Smooth lines (monotone cubic), a faded gradient area under the Live line, an end-point dot with a soft glow. *(POL-04, POL-05)*
11. Hover tooltip as a rounded card (time, value, zone chip) following the crosshair, instead of text in the header.
12. Sparkline end dots *(POL-05)*, subtle fade of axis labels. (Bars and bands leave History and the Live chart, D-0018.)

## Windows integration
13. ~~Optional "use Windows accent colour" for focus rings, selected tab and switches.~~ Dropped for now: ink is the accent (D-0017).
14. Taskbar overlay badge in the zone colour; acrylic on the flyout *(spike POL-08)*; jump list actions (Snooze 1 h, Open History).

## Layout and craft
15. A 4 px grid and concentric radii (inner radius = outer radius − padding).
16. Skeleton placeholders instead of spinners; restore the last view on launch; never shift layout while loading.
17. Per-room colour or icon in the room chip and menu.

## Delight (later)
18. Time-aware microcopy ("Last night was fair"), gentle celebration when air returns to Good, a compact always-on-top mini widget.
