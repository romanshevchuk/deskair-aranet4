# Accessibility

*Last reviewed: 2026-10*

- **Contrast:** text 4.5:1, chart colours 3:1, in light and dark. Re-check after any token change.
- **Not by colour alone:** every zone is also written as text and has an icon. History charts need patterns or icons per zone for colour-blind users (planned).
- **Keyboard:** every control reachable; visible focus ring; arrow keys move inside segmented controls; Esc closes the flyout and popups.
- **Screen readers (Narrator):** `AutomationProperties.Name` on charts (summarise current value and zone), icon buttons and the tray icon; live region announcement when the zone changes.
- **High Contrast:** use system colours when `SystemParameters.HighContrast` is on.
- **Motion:** only the toggle slide in v1; respect the Windows animation setting.
- **Scaling:** test 100, 125, 150 and 200 % and the minimum window size.
- **Language:** test Ukrainian for text overflow (see `localization.md`).
