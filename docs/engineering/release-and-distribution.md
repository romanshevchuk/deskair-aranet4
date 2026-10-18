# Release and distribution

*Last reviewed: 2026-10*

1. **Microsoft Store first** (MSIX; the step-by-step plan is `docs/plans/store-packaging.md`). Declare Bluetooth and full-trust capabilities, listing languages en-US and uk-UA, a privacy statement (no data leaves the PC), screenshots in both languages.
2. **GitHub releases** and **winget** later, with the same data folder (`Documents\Deskair`).
3. Versioning: SemVer. Keep a `CHANGELOG.md`.
4. Code signing is needed for the non-Store builds.
5. Store and non-Store builds differ in toast setup (package identity). Keep that difference in one place.
6. No telemetry in v1.

## Naming and discoverability

- Product name: **Deskair** (see `docs/decisions.md` D-0010). Reserve it in Partner Center early.
- Store: first sentence of the description is "Unofficial desktop monitor for the Aranet4 CO₂ sensor." Add "Aranet4" and "Aranet" to the search terms, add a short "not affiliated with the sensor's maker" line, and use screenshot captions such as "Your Aranet4 on your PC".
- GitHub: repository named after the brand when ready; description "Unofficial Windows monitor for Aranet4"; topics `aranet4`, `co2`, `air-quality`, `bluetooth-le`, `wpf`, `windows`; README title includes "Aranet4".
- winget: package ID like `<Publisher>.Deskair`, with `aranet4` in the tags.
- Do not put "Aranet" in the product name without the maker's permission.

