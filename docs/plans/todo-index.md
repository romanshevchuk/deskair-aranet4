# TODO index

*Last reviewed: 2026-10*

Every TODO card in the code, with its size and the model that should take it. Format and protocol: [m1-skeleton-brief.md](m1-skeleton-brief.md) sections 4 and 6. Update the status in the same change that removes the card.

- **Size:** S = a few lines of one method; M = one class or a small group of methods; L = timing, threading, Windows APIs or drawing.
- **Status:** `todo`, `doing`, `done`.
- **Test:** `skipped test` means a skipped test with reason `TODO(ID)` already exists; unskip it and make it pass. `manual / gallery` means the card is done when the gallery or a manual check shows it.
- Controls, views and Windows glue are stubs too: an empty control template, an empty view, or a method that throws `NotImplementedException("TODO(ID)")`. The card sits above the stub (in the XAML file for templates and views; for controls, in both the `.cs` and the `.xaml` file).

Totals: 0 cards.

| ID | File | Size | Suggested model | Status | Test |
|---|---|---|---|---|---|
