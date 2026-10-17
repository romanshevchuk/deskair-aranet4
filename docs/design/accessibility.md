# Accessibility

*Last reviewed: 2026-10-17*

- **Contrast:** text 4.5:1, chart colours 3:1, in light and dark. Re-check after any token change. Exception: the lightest heat steps of the History grid (design-tokens.md "Contrast", D-0018).
- **Not by colour alone:** every zone is also written as text and has an icon. In the History grid every value is also text: the row average, the tooltip and focus label of each square, the "best" / "worst" tags, the "Worst 17:00–20:00" label and the legend with its limits.
- **Keyboard:** every control reachable; visible focus ring (blue, not the accent); the header tabs are one Tab stop (arrow keys move between them), then the room button, then the page; **F6 / Shift+F6** jump between the header and the page, **Ctrl+1 / 2 / 3** open Live, History and Settings; arrow keys move inside segmented controls; Tab moves between the hero and the mini cards, which Narrator reads as selectable items; Esc closes the flyout and popups. History: Alt+← and Alt+→ step the period; Tab enters the grid on the latest day's current hour, arrow keys move between squares (Home / End to the row's ends), and the focused square shows its tooltip.
- **Screen readers (Narrator):** Charts are drawn elements, so `ChartBase` gives them a `ChartAutomationPeer` (an image with the name set in XAML; a chart without a name stays out of the tree). `AutomationProperties.Name` on charts (summarise current value and zone), icon buttons (Export CSV, Settings, ‹ and ›) and the tray icon; live region announcement when the zone changes and when the History period changes. Each grid square is announced as "Tue 13 Oct, 16:00 to 17:00, 1234 ppm, Elevated" ("no data" when empty); a collapsed row as "No readings from 15 Sep to 5 Oct".
- **High Contrast:** `ThemeService` lays `HighContrastPalette` over the tokens while `SystemParameters.HighContrast` is on (and again when it changes): surfaces are the window colour; text, borders and the card outlines the window text colour; dividers and gridlines the text colour at 50 %; the accent is the highlight pair; the focus ring is the hot-track colour. Zones, metrics and the heat steps become the text colour at stepped opacities (CO₂ six steps from 20 to 100 %; the comfort scales 15, 50 and 90 %; zones 45, 70 and 100 %), the zone glows disappear, so every value stays readable through its text, the tooltip or focus label and the legend. The tray icon is drawn separately and keeps its colours.
- **Motion:** the toggle slide, and from POL-16 the sliding segment highlight, the nav underline, the hero value fade, the gauge glide, the flyout open fade, the High pill dot pulse. All of them stop when Windows "Animation effects" is off (`SystemParameters.ClientAreaAnimation`).
- **Scaling:** test 100, 125, 150 and 200 % and the minimum window size.
- **Language:** test Ukrainian for text overflow (see `localization.md`), especially the History toolbar, the summary card and the grid's end column.

## Manual check (about 10 minutes, Windows 11)

The tests cover names (`AutomationAuditTests`), contrast and the High Contrast palette. They cannot say what Narrator reads or whether the focus order feels right, so run this before a release. Scope: Live, Settings and the flyout; History only for the period buttons.

1. **Keyboard only** (no mouse). F6 jumps between the header and the page and Ctrl+1 / 2 / 3 open Live, History and Settings. Tab through the main window: the header nav, the room button, then the page. Every stop shows the blue focus ring; nothing is skipped; Esc closes the room menu and the flyout. Enter or Space activates buttons and toggles; arrow keys move inside Live / History / Settings and the segmented controls.
2. **Live with Narrator** (Win+Ctrl+Enter). Tab to the hero ("CO₂, 1076 ppm, Elevated, selected"), to each mini card, to the 1h / 6h / 24h selector ("Time span"). Hear the chart read as "CO₂, last hour: low…, average…, high…". When the air changes zone, the zone word is announced.
3. **Settings with Narrator.** Each toggle, segmented control and time field is read with its label and state ("Alerts, on").
4. **Flyout.** Open it from the tray (Win+B, arrows, Enter on the icon). Narrator reads the room, the value, the advice, the three readings, Snooze, Settings and Open Deskair; focus lands inside it; Esc closes it and returns to the taskbar.
5. **Scale.** Set Windows display scaling to 150 % and 200 %, and text size to 150 %: no text is cut off on Live or Settings, and the window can still be used at its minimum size.
6. **High Contrast** (Alt+Left Shift+PrintScreen). Live and Settings stay readable; focus is visible.

Write down anything that failed: what you pressed, what Narrator said, and what you expected.
