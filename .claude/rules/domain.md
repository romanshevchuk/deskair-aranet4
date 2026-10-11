---
paths:
  - "src/Deskair.Domain/**"
---
<!-- Last reviewed: 2026-10 -->

# Domain rules

Types and pure calculations. `net10.0`. May depend on nothing.

- Never: files or I/O, `DateTime.Now` or `TimeProvider` (time is passed in), UI, Bluetooth, user-visible text, `System.Windows`.
- Immutable `record`s; pure functions; no static state.
- Every public calculation has tests, including empty periods, partial buckets and daylight-saving days.
- Timestamps are UTC; calendar days and months use the user's local time zone.
- Buckets are half-open: start inclusive, end exclusive.
- An average is the mean of the existing readings, equally weighted.
- Coverage is readings present divided by expected slots. Under 80 % is "partial", none is "no data".
- Spec: `docs/product/behaviour-rules.md`.
