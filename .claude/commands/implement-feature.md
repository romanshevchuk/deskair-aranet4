---
description: Plan and implement one backlog item
argument-hint: <backlog ID, for example LIVE-03>
---

Implement backlog item $ARGUMENTS.

1. Read the item in `docs/plans/backlog.md` and every doc it links to.
2. Write a short plan: files to change, new types, tests, strings to add (English and Ukrainian). Wait for my approval.
3. Implement in small steps. Use theme resources for colours and spacing, and resource strings for text.
4. Add tests. Run `dotnet build` and `dotnet test`.
5. Update the docs that describe the changed behaviour, and set the backlog status.
6. Summarise what changed and anything that differs from the docs.
