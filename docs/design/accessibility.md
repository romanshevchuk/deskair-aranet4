# Accessibility

*Last reviewed: 2026-10-17*

- **Contrast:** text 4.5:1, chart colours 3:1, in light and dark. Re-check after any token change. Exception: the lightest heat steps of the History grid (design-tokens.md "Contrast", D-0018).
- **Not by colour alone:** every zone is also written as text and has an icon. In the History grid every value is also text: the row average, the tooltip and focus label of each square, the "best" / "worst" tags, the "Worst 17:00–20:00" label and the legend with its limits.
- **Keyboard:** every control reachable; visible focus ring (blue, not the accent); arrow keys move inside segmented controls; Tab moves between the hero and the mini cards, which Narrator reads as selectable items; Esc closes the flyout and popups. History: Alt+← and Alt+→ step the period; Tab enters the grid on the latest day's current hour, arrow keys move between squares (Home / End to the row's ends), and the focused square shows its tooltip.
- **Screen readers (Narrator):** `AutomationProperties.Name` on charts (summarise current value and zone), icon buttons (Export CSV, Settings, ‹ and ›) and the tray icon; live region announcement when the zone changes and when the History period changes. Each grid square is announced as "Tue 13 Oct, 16:00 to 17:00, 1234 ppm, Elevated" ("no data" when empty); a collapsed row as "No readings from 15 Sep to 5 Oct".
- **High Contrast:** use system colours when `SystemParameters.HighContrast` is on; the heat steps then become the system text colour at stepped opacities, and the values stay readable through focus labels.
- **Motion:** the toggle slide, and from POL-16 the sliding segment highlight, the nav underline, the hero value fade, the gauge glide, the flyout open fade, the High pill dot pulse. All of them stop when Windows "Animation effects" is off (`SystemParameters.ClientAreaAnimation`).
- **Scaling:** test 100, 125, 150 and 200 % and the minimum window size.
- **Language:** test Ukrainian for text overflow (see `localization.md`), especially the History toolbar, the summary card and the grid's end column.
