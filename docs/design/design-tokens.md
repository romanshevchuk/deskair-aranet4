# Design tokens

*Last reviewed: 2026-10-14*

Design size is 1200 × 728 px at 100 % scale. Implement every value as a named resource in a light and a dark `ResourceDictionary`. The visual target is `design/polish/deskair-polish-concept.html` (POL-00); where it and this file differ, this file wins.

## Neutrals

| Token | Light | Dark | Use |
|---|---|---|---|
| window.bg | #EEF1F3 | #12161A | App background (Mica fallback) |
| surface | #FFFFFF | #1B2026 | Cards, popovers |
| border | #DCE2E6 | #2C343C | Outlines |
| card.border | #EAEBEC | #2B3035 | Card outline: 9 % ink (light) or 7 % white (dark) over the surface, 1 px (prototype `.card`). Every card, the hero included |
| card.border.hover | #B3BDC4 | #4A545D | Mini card outline on hover |
| divider | #E6EAED | #262D34 | Lines inside cards, the hero's stat dividers included |
| grid | #E3E8EB | #232A31 | Chart gridlines, the night band |
| segment.bg | #E4E9EC | #232A31 | Segmented track |
| segment.selected | #FFFFFF | #323A43 | Selected segment (1 px border) |
| text.primary | #182026 | #EEF2F5 | Main text |
| text.secondary | #5A6670 | #9AA5AF | Labels |
| text.tertiary | #5B6771 | #8D99A4 | Axis, quiet labels |
| text.strong2 | #35414A | #CBD3DA | Chart annotations |
| insight.bg | #F1F4F5 | #222930 | Insight box |
| accent | #182026 | #EEF2F5 | The app's one accent, ink (D-0017): nav underline, toggles on, primary button, selected mini card ring, worst-hours outline in the grid |
| accent.on | #FFFFFF | #12161A | Text and the toggle knob on `accent` |
| toggle.off | #C3CBD1 | #3E4852 | Switch track, off (on uses `accent`) |
| focus.ring | #2563C9 | #6E9BE0 | Keyboard focus; stays blue, not the accent |
| flyout.band | #09141C24 | #08FFFFFF | Flyout footer band: 3.5 % ink (light) or 3 % white (dark) over the flyout |

Green is never used for controls: it only means Good air.

## Zone colours

| Token | Good (L / D) | Elevated (L / D) | High (L / D) |
|---|---|---|---|
| glow (hero) | #2B3FA77E / #1C4CC08C | #33F0B03C / #1CF0B74A | #29DE5C4E / #21F07A6E |
| glow (flyout) | #4D3FA77E / #2E4CC08C | #57F0B03C / #2EF0B74A | #47DE5C4E / #36F07A6E |
| pill.bg | #D3EFE1 / #1F4A38 | #FCEBC4 / #4A3A12 | #F9D9D4 / #4F2420 |
| pill.text | #14573A / #9FE0BF | #7A4800 / #F2CE7A | #8E241A / #F5A9A0 |
| dot | #1E8A5C | #D4961A | #C7453A |
| advice.bg | #DDF2E7 / #1B3328 | #FBEFD0 / #33291A | #FCE2DD / #35201D |
| advice.icon | #14573A / #9FE0BF | #8A5300 / #F2CE7A | #8E241A / #F5A9A0 |

**Glow:** a radial gradient over the `surface`, centred on the top-left corner, from the glow colour to transparent. Hero: radius 130 % of the width by 75 % of the height, transparent at 60 %. Flyout: 120 % by 55 %, transparent at 70 %. Not connected uses #1A78828C (light) / #0F96A0AA (dark) in both places. The hero and zone cards have no zone fill, no zone border and no zone-tinted shadow (D-0017); the advice panel has a fill and no border.

Gauge and ribbon segments: #3FA77E, #E6B04B, #C7453A. Chart lines: #1E8A5C, #C47F00, #C23B2B. Trend text: falling #1B7A53 (dark #6FD3A3), rising #B3342A (dark #FF8A7E). The gauge scale is 400 to 2000 ppm.

**Live chart:** no zone bands. A threshold (1000 or 1400) is a 1 px dashed line (4 on, 4 off) in the zone dot colour at 60 %, with a label in the zone's `pill.text` ("Elevated from 1000", "High from 1400") right-aligned above it (below it on the top edge), drawn only when the threshold is inside the axis or on its edge. Area under the line: the current zone's line colour, 22 % at the line to 0 % at the bottom.

## Heat scale (History grid)

CO₂ in six steps, lighter to darker as the level rises, two per zone (D-0018). A semantic scale: always shown with its legend.

| Token | From (ppm) | Light | Dark |
|---|---|---|---|
| heat.g1 | below 800 | #DDF2E7 | #1D3B30 |
| heat.g2 | 800 | #B4E0C9 | #2B6650 |
| heat.e1 | 1000 | #F0CC78 | #77612C |
| heat.e2 | 1200 | #E3A23C | #B88A35 |
| heat.h1 | 1400 | #E07462 | #D06A5C |
| heat.h2 | 2000 | #C23B2B | #F08A7C |
| heat.none.line | no data | #29141C24 | #29FFFFFF |

No data: an empty square with a 1 px `heat.none.line` outline. Other metrics: a single-hue scale per metric (POL-11 follow-up; not designed yet).

## Metric colours

| Metric | Light / Dark | Unit | Ideal range |
|---|---|---|---|
| CO₂ | #D4961A (dot) | ppm | zones |
| Temperature | #E07A1F / #F2A15E | °C (°F option) | 20 to 24 °C |
| Humidity | #2563C9 / #6E9BE0 | % | 30 to 50 % |
| Pressure | #6D4FC2 / #A58DF0 | hPa | none |

Non-CO₂ Live charts: plot area filled with the metric colour at 7 % opacity, ideal range at 17 %, label "ideal 30 to 50". Sparklines: line 1.75 px in the metric colour, area 18 % to 0 %, end dot 5.5 px.

## Typography

Segoe UI Variable Text, falling back to Segoe UI; display sizes use Segoe UI Variable Display. Minimum size 12 px. Text sizes 12 / 13 / 14 / 16 / 20, display sizes 32 / 48 / 56 / 104; nothing in between.

**Figures (D-0020):** standalone display and stat values use proportional figures (tabular figures give each "1" a full digit width and open gaps, as in "1151"). Numbers that line up in columns (grid row averages, chart summaries) and axis labels use tabular figures.

| Role | Size / weight | Figures |
|---|---|---|
| Hero value | 104 px, 300, Display | Proportional |
| Flyout value | 56 px, 300, Display | Proportional |
| History average | 48 px, 300, Display | Proportional |
| Mini card value | 32 px, 500, Display | Proportional |
| Hero stat values | 20 px, 600 | Proportional |
| Titles, summary values, flyout readings | 16 px, 600 | Proportional |
| Body, hero title | 14 px (hero title 600) | |
| Secondary, controls, mini card label (600) | 13 px | |
| Captions, chart text, section labels | 12 px. The HTML prototype shows labels uppercase with .06 em spacing; WPF has neither, so labels use normal case and spacing | Tabular on axes |

## Spacing, radius, elevation

| Token | Value |
|---|---|
| Header / footer height | 52 px / 40 px |
| Page padding | 24 px sides, 8 px top, 20 px bottom |
| Card gap | 16 px |
| Radius: card, hero | 16 px |
| Card elevation | A soft shadow behind every card, the hero included: light 0 3 16 at 9 % ink, dark 0 4 18 at 40 % black (the prototype's two layers merged: WPF has one shadow per element). The shadow is drawn by a separate border behind the content, so a changing chart inside a card does not re-blur it. No zone tint (D-0017) |
| Radius: advice panel, banner, insight | 12 px and 10 px |
| Radius: segmented track / segment | 10 px / 8 px |
| Radius: pill, toggle track, focus ring | Half the control height, so both ends are half circles (prototype: border-radius 999). WPF clamps a large radius on each axis separately, which gives elliptical ends, so `PillRadiusConverter` binds the radius to the height |
| Radius: window | 10 px (Windows 11); 1 px border at 30 % grey |
| Header nav | Text tabs 14 px, 26 px apart; the current one in `text.primary` 600 with a 18 × 3 px `accent` underline (radius 2) 9 px above the header's bottom edge; others `text.secondary`. Room chip: no border or shadow, hover background `segment.bg` |
| Hero padding | 22 / 24 / 20 px. Content groups spread over the height (top block, gauge, advice, stats) instead of a gap above the stats |
| Gauge marker | 20 px, `surface` fill, 3 px `text.primary` ring, soft shadow 0 2 6 at 18 % black |
| Settings | One column, at most 720 px wide, centred. Section titles 14 px 600 above their cards. Card padding 12 / 22 / 2 px; row at least 40 px tall with 4 px above and below, 16 px between label and control, a 1 px divider above every row but the first. Time fields 88 px wide |
| Live grid | 3 equal columns; rows 1 fr and 132 px |
| History | Toolbar 40 px: metric segmented control, period navigator (title 15 px 600, at least 176 px wide, 32 px arrow buttons), range segmented control, 34 px export icon button. Main card padding 18 / 22 / 12. Summary card 330 px wide, padding 20 / 22, full height |
| History grid | Label column 84 px; end column 160 px (row average right-aligned 46 px in, zone bar 60 px wide and up to 6 px high at 56 px, tag at 124 px); 2 px between squares; square radius 4 px or a quarter of its height if smaller; row height 9 to 44 px from the space available; a collapsed run is one 28 px row; the typical row follows a 16 px separator; legend band 50 px; the night bracket above the hour labels |
| Flyout | 360 px wide, radius 8 px (as the Windows flyouts), 1 px `card.border` outline, shadow 0 16 40 at 30 % black (WPF: blur 32, depth 10, opacity 0.28). Content padding 16 px at the sides. Footer band in `flyout.band`, padding 10 / 12, a 1 px `divider` above it. Ribbon 8 px high, segments radius 3 px, 2 px apart |
| Toast | drawn by Windows (card, fonts, corners); Deskair supplies the zone-coloured ring (96 px image, the same ring as the tray icon) and the texts; no left edge, no custom colours |

## Icons

Logo and tray ring: 28 × 28 viewbox, circle r 11, stroke 2.75 to 3, centre dot r 4.5, in the zone colour (the zone dot colours, the same in both themes). Tray icon not connected: dashed ring and slash in `Tray.Offline` (#7B858D). Tray icon with the value: rounded square in the zone colour with the CO₂ in thousands ("1.1k"), text `Tray.Value.OnLight` (#FFFFFF) on green and red and `Tray.Value.OnAmber` (#182026) on amber. Advice icons: 18 × 18 line icons (window, check circle, warning triangle), 1.6 px stroke, in `advice.icon`, without a badge behind them. Window glyph: 10 × 10. Battery: 26 × 12; green above 20 %, amber at 20 % or below, red at 10 % or below.

## Contrast

All text pairs reach 4.5:1 and all chart colours 3:1, in both themes. Re-check if a token changes.

**Exception (D-0018):** the heat steps `heat.g1` and `heat.g2` (light) and `heat.g1` (dark) are below 3:1 against the surface on purpose: fresh air is meant to recede. The grid's values are always available as text: each row's average, the tooltip and keyboard focus label of every square, and the Narrator label (accessibility.md).
