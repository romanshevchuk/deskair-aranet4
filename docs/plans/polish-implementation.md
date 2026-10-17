# Polish rework: implementation plan

*Last reviewed: 2026-10-15*

How to build the changes in [`ui-polish-plan.md`](ui-polish-plan.md) (what and why) to the look in [`design/polish/deskair-polish-concept.html`](../../design/polish/deskair-polish-concept.html) (the visual target, like `design/prototype/index.html` was for v1). Backlog IDs are `POL-00` to `POL-17` in [`backlog.md`](backlog.md); run each with `/implement-feature POL-xx`.

## Rules for every item

| Rule | Detail |
|---|---|
| Source of truth | The design docs, once POL-00 has updated them. Where the concept page and the docs differ, the docs win; note the difference in the item's summary. |
| One item, one session, one commit | Start each item in a fresh session. Don't run two items that touch the same files at the same time (see "Parallel work"). |
| Before and after renders | Run the render tests (`$env:DESKAIR_RENDER=1; dotnet test tests/Deskair.Tests --filter "FullyQualifiedName~RenderTests"`) before starting and after finishing, and list which images changed in the summary. |
| No extra design | If the spec leaves a visual choice open, stop and ask; don't invent. Put unanswered points in `questions.md`. |
| Done means | `dotnet format`, `dotnet build`, `dotnet test` pass; gallery updated for new or changed controls (light, dark, English, Ukrainian); docs and backlog status updated. |
| Review gate | At the end of each phase, an Opus session runs `/review-against-design <area>` against the concept page and the renders. Fixes become follow-up items. |

## Which model does what

| Give to Opus when the item… | Give to Sonnet when the item… |
|---|---|
| has visual or interaction decisions the docs don't pin down | has a spec that answers every visual question |
| creates a new reusable pattern (a chart type, a period model, a token scheme) | repeats a pattern that already exists in the code |
| changes tokens that must be judged by eye in both themes | is mostly XAML layout, strings, formatting or deletion guarded by tests |
| has data or time edge cases (DST, partial days, gaps) | has its rules already written in behaviour-rules.md |
| is a review of other work | |

Effort: Opus items at high effort; Sonnet at its default. For Sonnet items, read the plan step of `/implement-feature` carefully before approving: that is where drift from the spec shows.

## Phases and items

**Size:** S is under a day of agent work, M one or two sessions, L several sessions. **Depends:** items that must be merged first.

### Phase 0: decisions into the docs

| ID | Item | Plan refs | Depends | Size | Model |
|---|---|---|---|---|---|
| POL-00 | Update the design docs to the new direction and record the decisions | all | none | M | Opus |

POL-00 must come first: every later item, and every Sonnet session, reads the docs. If they still describe range bars and the Night filter, that is what gets built.

| Doc | Changes |
|---|---|
| `docs/design/design-tokens.md` | Zone glow tokens (`Zone.*.Glow`, both themes); hero and advice lose zone borders and tinted shadows; ink as the app accent (toggles, primary button, selected states), focus ring stays blue; six heat steps `Heat.G1` to `Heat.H2` and `Heat.None.Line`; flyout radius 8 and lighter shadow; flyout footer band; type scale 12 / 13 / 14 / 16 / 20 / 32 / 104. |
| `docs/product/behaviour-rules.md` | Number format: no group separator for any metric value; proportional figures on display values; percent "59%" in English, "59 %" in Ukrainian. Periods for ‹ ›. Night is a band and a summary section, no filter. "Air improved" markers use the documented airing rule, at least 60 minutes apart. Worst-hours tip wording when the window falls in the night hours. Humidity "a bit" / "too" labels. |
| `docs/design/screens-and-states.md` | Live: metric chosen by the hero and mini cards, tabs removed, cards keep their places. Flyout: no inner card, footer band, 8 px ribbon, round-hour labels, Not connected with the last reading. History: controls table (metric, range, ‹ ›, export), ranges (Day = rolling 24 h then calendar days; Week; Month = 30 days; Year = 12 months), the grid, the empty state, the summary card. |
| `docs/design/components.md` | New: `HourGridChart`, `PeriodNavigator`, `SummaryCard` (replaces `StatsCard` and `SleepCard`). Removed: `MetricTabs`, `TypicalDayChart`, range bars in History. |
| `docs/decisions.md` | D-0017 ink accent; D-0018 History grid; D-0019 period navigator, Night filter and Group by removed; D-0020 no group separators, proportional display figures; D-0021 humidity labels. |
| `design/prototype-shot.ps1` | Optional: accept the concept page so its states can be shot at render scale for side-by-side review. |

### Phase 1: foundation

| ID | Item | Plan refs | Depends | Size | Model |
|---|---|---|---|---|---|
| POL-01 | Surfaces and accent: hero glow per zone, no zone border or tinted shadow, advice fill without border, ink accent on toggles and primary button, gauge knob, flyout radius and shadow | 1, 4, 14, F6 (corners and shadow) | POL-00 | M | Opus |
| POL-02 | Numbers and units: ppm without separator, proportional hero figures, percent and time formats, "Elevated so far" label | 3, Small things | POL-00 | S | Sonnet |
| POL-03 | Shell chrome: text nav with underline, plain room chip, footer without the hardware ID | 6, 13 | POL-01 | S | Sonnet |

POL-02 spec: one formatter for ppm and one for percent, culture-aware, with unit tests for English and Ukrainian; all view models use them (search for `"N0"` and `%` in `ViewModels`). `Text.HeroValue`, `Text.HistoryAverage` and the flyout value set `Typography.NumeralAlignment` to `Proportional`.

### Phase 2: Live

| ID | Item | Plan refs | Depends | Size | Model |
|---|---|---|---|---|---|
| POL-04 | Live chart: y axis fitted to the data (min span 200 ppm, nice ticks), threshold line with label only when in range, area fill in the current zone, monotone smoothing | 2 | POL-01 | M | Opus |
| POL-05 | Sparklines: minimum visible range per metric, area fill, end dot, same size in all cards | 7 | POL-04 | S | Sonnet |
| POL-06 | Metric selection by the hero and mini cards; remove `MetricTabs`; keyboard and Narrator path | 5 | POL-04 | M | Opus |

POL-05 spec: reuse POL-04's smoothing (`ChartLines`) and area-fill layer; minimum spans 1 °C, 6 % RH, 2 hPa, centred on the data.

### Phase 3: flyout

| ID | Item | Plan refs | Depends | Size | Model |
|---|---|---|---|---|---|
| POL-07 | Flyout: no inner card, zone glow, single-line advice, 8 px ribbon with round-hour labels, quiet readings, footer band, Not connected with last reading and one step | F1 to F5 | POL-01, POL-02 | M | Sonnet |
| POL-08 | Spike: acrylic flyout (non-layered window, `DWMWA_SYSTEMBACKDROP_TYPE`), keep or drop | F6 | POL-07 | M | Opus, optional |

POL-07 spec: the concept page's flyout section is the layout; strings `Offline_LastReading` and `Offline_Step_Wake` exist; the ribbon labels sit at their true positions (18:00, 00:00, 06:00) plus "Now" right-aligned, using the same positioning as the Live ribbon.

### Phase 4: History

| ID | Item | Plan refs | Depends | Size | Model |
|---|---|---|---|---|---|
| POL-09 | Period model and navigator: range + offset → start, end, label, previous period; rolling 24 h then calendar days; ‹ › with limits; empty state with "Go to the latest"; Alt+← / Alt+→ | H1 | POL-00 | M | Opus |
| POL-10 | Toolbar: metric and range only; remove the Night filter from the UI and the now-unused code paths of the Night filter and Group by (keep `NightWindow`; the Group by controls already went in POL-09 to make room for the navigator); remove the unused strings (`Toolbar_GroupBy`, `Group_By*`, `Hours_*`); export as an `IconButton` | H2 | POL-09, POL-14 | S | Sonnet |
| POL-11 | `HourGridChart` for Week and Month: hourly values per day from the query service, six heat steps, row average, zone split, best / worst tags, typical row with the worst three hours, collapsed gap runs, legend, hover and keyboard focus | H3, H4, H5 | POL-09 | L | Opus |
| POL-12 | Year as a months × days grid on `HourGridChart` (daily average, at least 6 hours) | H6 | POL-11 | S | Sonnet |

POL-12 spec: a Domain function like `HourGridCalculator.Build` that returns month rows of up to 31 daily means (a day needs readings in at least 6 hours); `HourGridBuilder` gets a Year path that sets `Columns = 31`, day-number column labels (1, 5, 10 … 31; centre them, a small change in `HourGridLayer.DrawTop`), no night bracket and no typical row, best / worst month tags (`MinimumHoursForTag` becomes a parameter: 240 hours for a month), and `UnitText` "Each square is one day". `HistoryViewModel` sets `wantGrid` for Year too.
| POL-13 | Day chart: typical line (previous 7 days), night shading, "air improved" markers from a Domain function with tests, hover with the typical value | H7 | POL-09 | M | Sonnet |
| POL-14 | Summary card: merge `StatsCard` and `SleepCard`; change as text; coverage as a line; Nights section (built, Q49); highest and lowest with time; one tip with night-aware wording | H8, H9 | POL-11 | M | Sonnet |

POL-09 notes: a Domain type (for example `HistoryPeriod`) with tests across the spring and autumn clock changes and the first-reading limit; the query service already serves arbitrary ranges for export.

POL-11 notes: Opus builds the control, its chart model and the hourly-per-day query once, generic enough that POL-12 only supplies a different model (rows of months, columns of days). Collapse rule: two or more consecutive days without readings become one row; one missing day stays as outlined squares. Rows under 14 px show labels on Mondays and Today only. Heat steps are a semantic scale and always show their legend. **Built:** `HourGridCalculator` (Domain), `ReadingQueryService.GetHourGridAsync`, `HourGridBuilder` and `HourGridModel` (App), `HourGridChart` with `HourGridLayout`, `HourGridLayer` and `HourGridLegendLayer`, the `Heat.*` tokens, a gallery sample. CO₂ with All day only (see screens-and-states.md).

POL-13 spec: marker rule from behaviour-rules.md "Air improved" (detection and 60-minute spacing); the typical line is the hourly mean of the 7 days before the shown day; night shading uses `NightWindow`.

POL-14 spec: the time of the highest and lowest reading needs timestamps in the buckets (Q47 "Not built"); add them to the query result first, with tests. Best and worst day are no longer in the card (they are tags in the grid). The stats card still shows best and worst day; remove them there now that the grid tags the rows. **Built:** the Nights section for Week, Month and Year (Q49): `NightsCalculator`, `ReadingQueryService.GetNightsAsync` with tests for the count, the average, the best and worst start times and the empty window, and `SummaryCard` between the zones and "Highest reading".

### Phase 5: the rest

| ID | Item | Plan refs | Depends | Size | Model |
|---|---|---|---|---|---|
| POL-15 | Settings: one centred column (max 720 px), section titles above the cards, compact time fields, "Add" as a secondary button | 12 | POL-01 | S | Sonnet |
| POL-16 | Motion: sliding selection in segmented controls and the nav underline, hero value cross-fade, gauge glide, flyout open fade; off when Windows animations are off | 15; polish-ideas 3, 6, 8 | Phases 1 to 4 | M | Opus |
| POL-17 | Final review of all screens against the concept page, both themes, both languages; then hand over to A11Y-01 and I18N-02 | all | POL-16 | S | Opus |

## Order and parallel work

```
POL-00 ─► POL-01 ─┬─► POL-02 ─► POL-03
                  ├─► POL-04 ─► POL-05, POL-06            (Live)
                  ├─► POL-07 ─► POL-08                    (Flyout)
                  └─► POL-09 ─► POL-10, POL-13
                                POL-11 ─► POL-12, POL-14  (History)
                  POL-15 any time after POL-01
                  POL-16 ─► POL-17 last
```

After POL-01 is merged, the Live, Flyout and History streams touch different files and can run in parallel, each in its own worktree. Within a stream, keep the order. POL-02 touches view models across all three streams, so merge it before starting the others if possible.

## Risks

| Risk | Mitigation |
|---|---|
| Sonnet "fixes" new designs back to old docs | POL-00 first; each spec names the doc section it follows. |
| Dark theme glows and heat steps look muddy | POL-01 and POL-11 are Opus items judged on dark renders; check the heat steps with a colour-blindness simulator before merging POL-11. |
| Removing the Night filter loses a feature people use | The Nights section and the night band keep the information; recorded as a decision in POL-00, revisit after a week of use. |
| `HourGridChart` gets slow with many rows | At most 31 rows × 24 squares (744 rectangles); draw in one `DrawingVisual` like the other charts, hit-test by row and column arithmetic. |
| Ukrainian labels overflow in the summary card and grid | Check the Ukrainian renders in every item; I18N-02 native review after POL-17. |

### Phase 4b: History per metric (added after POL-11 to POL-14)

| ID | Item | Plan refs | Depends | Size | Model |
|---|---|---|---|---|---|
| POL-18 | Each metric gets the History chart that fits it (concept page, History, note 9) | H3, H7, H8 | POL-11 to POL-14 | M | Opus for the docs and tokens, then Sonnet |

**Bugs first (S): done.** On the Temperature, Humidity and Pressure tabs, the Typical day card shows CO₂ data (1000 / 1400 axis, "worst 17–20h"), and Pressure shows a sleep section ("Deep sleep avg 990.6 hPa"). Neither belongs there.

| Metric | Week, Month, Year | Day | Summary card |
|---|---|---|---|
| CO₂ | As built (zones grid) | As built | As built |
| Temperature | The hour grid with a diverging comfort scale: two cool steps (below 18, 18–20 °C), a neutral grey for comfortable (20–24), two warm steps (24–26, 26 and above). Row end: average and a cool / comfortable / warm bar (comfortable in the Good segment colour); best and worst = most and least time comfortable. Typical row outlines the three hours furthest outside the range and names them ("Coolest 03:00–06:00") | Line in the metric colour, the comfort range as a soft band (metric colour 9 %, "comfort 20–24 °C"), typical line, night band; no area fill | Average with a comfort pill (Comfortable in Good colours; "A bit cool / Too cool", "A bit warm / Too warm" in Elevated colours); change as neutral text ("warmer / cooler than…"); Time in comfort range; Nights (average, coolest, warmest); highest and lowest; tip names the coolest or warmest hours, or "Comfortable around the clock" |
| Humidity | As temperature, with dry (below 25, 25–30 %) and humid (50–60, 60 and above) steps; "Driest" / "Most humid" | As temperature, "comfort 30–50 %" | As temperature; the humid tip suggests airing after cooking or a shower; the dry one says it is usual in heated rooms in winter |
| Pressure | No grid and no typical day: one line of hourly means (daily means for Year) with a fitted axis and the biggest 24-hour change shaded and labelled ("+14.8 hPa in 24 h") | Line, fitted axis, no night band, no typical line | Average, change as neutral text, Biggest change in 24 hours with its start, the range of the period, and one line: "Pressure follows the weather, not the room" |

New tokens: `Heat.Cool2`, `Heat.Cool1`, `Heat.Comfort`, `Heat.Warm1`, `Heat.Warm2`, `Heat.Dry2`, `Heat.Dry1`, `Heat.Humid1`, `Heat.Humid2` (values in the concept page's `--hm-c*`, `--hm-ok`, `--hm-w*`, `--hm-d*`, `--hm-m*`, light and dark). `HourGridModel.Steps` already carries the limits; the bins and the end-bar keys need to come from the metric. Update design-tokens.md "Heat scale", screens-and-states.md "History views" and behaviour-rules.md (comfort steps, "furthest outside the range" for the typical row) before building.
