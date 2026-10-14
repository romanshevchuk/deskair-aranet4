# Brief: UI/UX review of Deskair

*Last reviewed: 2026-10-14*

For a reviewing model or designer. Deskair is a Windows tray app (WPF, .NET 10) that shows the CO₂, temperature, humidity and pressure of the owner's Aranet4 sensor: a Live screen, a History screen, Settings, a tray flyout and Windows toasts. The owner wants a short list of concrete, ranked improvements, not a redesign.

## What to look at, in this order

1. **Pictures first.** Run `DESKAIR_RENDER=1 dotnet test tests/Deskair.Tests -c Release --filter "FullyQualifiedName~RenderTests"` and open `artifacts/visual/`: `live-*`, `history-*`, `settings-*`, `flyout-*`, `firstrun-*`, in light and dark, English and Ukrainian. These are the app as it draws itself. Judge what you see before reading code.
2. **The target look.** `design/prototype/index.html` is the intended design; `design/prototype-shot.ps1` (see its header) screenshots its window in any state at the same scale as the renders, so the two can be compared side by side. Where the app differs from the prototype on purpose, Q47 in `docs/plans/questions.md` says so (WPF cannot do uppercase and letter spacing; the Work hours filter is dropped from v1).
3. **The rules.** `docs/design/design-tokens.md` (colours, sizes, radii, elevation), `docs/design/screens-and-states.md` (every screen and state), `docs/design/accessibility.md`, `docs/product/behaviour-rules.md`. A suggestion that breaks a token or a rule should say so.
4. **Code only to confirm or cost a suggestion.** XAML is under `src/Deskair.App/Ui/` (Controls, Views, Charts, Theme tokens); view models under `src/Deskair.App/ViewModels/`.

## What to judge

- Hierarchy and scanning: is the answer to "is my air OK right now?" visible within a second on Live and in the flyout?
- Consistency: spacing, radii, type sizes and colour use across screens; anything that looks accidental.
- History: can a person tell a good night from a bad week at a glance? Are the legends, labels and empty and partial states clear?
- States: not connected, stale, low battery, first run, empty History, Do Not Disturb, snoozed.
- Accessibility: contrast in both themes, text size, keyboard focus, what Narrator would say.
- Ukrainian text length and the tone of the copy (the Ukrainian is a draft).
- Small things the owner may not have noticed (alignment, clipped text, cursor and hover states).

## What the review cannot see

Only static renders exist for the app: no hover, no animation, no real window chrome, no multi-monitor or other DPI scales, and the Live data is simulated. The tray menu and Windows toasts are drawn by Windows. Say when a point depends on something you could not see.

## How to answer

A ranked list. For each item: where (screen and element), what is wrong, the change in one or two sentences, how much work it is (S, M or L), and whether it keeps to the design tokens. Put the five most valuable first. Do not edit files; the owner decides what to build.
