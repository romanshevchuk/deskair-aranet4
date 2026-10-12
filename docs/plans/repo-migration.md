# Repository migration plan

*Last reviewed: 2026-10*

Goal: turn the existing `aranet4-monitor` repository into `deskair`, start the rewrite on a clean branch, keep the old code available as a reference but outside the agents' working tree.

## Steps

1. **Rename on GitHub:** Settings, General, Repository name, `deskair`. Old links redirect. Do not create another repository called `aranet4-monitor`.
2. **Tag the old code** in your existing local clone (`..\Dev\Projects\AranetHomeDesktop`), then update its remote:
   ```
   git remote set-url origin https://github.com/romanshevchuk/deskair.git
   git tag legacy-v0
   git push origin legacy-v0
   ```
3. **Two fresh clones** side by side, in your projects folder:
   ```
   git clone https://github.com/romanshevchuk/deskair.git deskair
   git clone --branch legacy-v0 https://github.com/romanshevchuk/deskair.git deskair-legacy
   ```
   `deskair` is the working folder. `deskair-legacy` is read-only reference (detached at the tag).
4. **Start the rewrite on an empty branch** (so the old code is not in the working tree):
   ```
   cd deskair
   git switch --orphan rewrite
   ```
   Unzip the docs package into the folder, then:
   ```
   git add -A
   git commit -m "docs: initial documentation"
   git push -u origin rewrite
   ```
5. **Skeleton (Sonnet).** Branch `feature/M1-skeleton` from `rewrite`. Start Claude in `deskair` only, with the brief `docs/plans/m1-skeleton-brief.md`. Do not give it access to `deskair-legacy`. Review the result and merge into `rewrite`.
6. **Spikes M0 (Sonnet).** Start Claude in `deskair` with `--add-dir ../deskair-legacy` and point it at the specific legacy files about Bluetooth. Task: adapt the ideas to `IBleConnection` and our parser pattern, with tests from real captures; never copy wholesale. Compare results with a phone export.
7. **Fill in the TODO cards (Haiku or Sonnet)** one at a time, from the docs. The legacy repo is only for cards that say so.
8. **Make the rewrite the main line** once Live and History work: on GitHub rename branch `main` to `legacy`, rename `rewrite` to `main` (or change the default branch), then update the README, repository description and topics (`aranet4`, `co2`, `air-quality`, `bluetooth-le`, `wpf`, `windows`).
9. **Before publishing:** reserve the Store name, check trademarks and domains (see `docs/decisions.md` D-0010).

## Why this order

- The orphan branch makes "do not read the old code" true by construction; telling an agent to ignore files it can see is unreliable.
- A separate read-only clone lets you grant legacy access deliberately and only for BLE work.
- The tag and a later `legacy` branch keep the old app recoverable.
