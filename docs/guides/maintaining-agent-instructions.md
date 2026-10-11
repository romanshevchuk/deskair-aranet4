# Maintaining agent instructions

*Last reviewed: 2026-10*

Based on the Claude Code memory documentation (code.claude.com/docs/en/memory). Claude treats instruction files as context, not enforced configuration, so wording and placement decide how well they work.

## Where each kind of instruction goes

| Kind | Where | Loads |
|---|---|---|
| Facts needed in every session (project, commands, layout, "always do X", prohibitions) | `AGENTS.md` (shared) imported by `CLAUDE.md` | At launch |
| Rules for one area of the code | `.claude/rules/<area>.md` with `paths:` frontmatter | When Claude reads, edits or creates a matching file |
| Rules for all files without a path scope | `.claude/rules/` without `paths` | At launch |
| A multi-step procedure | A skill (`.claude/skills/<name>/SKILL.md`), backed by a guide in `docs/guides/` | When invoked or judged relevant |
| A fixed command or review routine | `.claude/commands/` | When typed |
| Something that must be blocked or must always run | A permission rule or a hook in `.claude/settings.json` | Enforced by Claude Code, not by Claude |
| Reference material (specs, decisions) | `docs/` | On demand |

Nested `CLAUDE.md` files per folder load only when Claude reads a file there, so a new file in an unread folder can miss them. Path-scoped rules also trigger on write and edit, are kept in one place, and are easier to audit. We use rules, not nested files.

## Writing rules

- Concrete and checkable: "Use `DynamicResource` tokens, no literal colours", not "style properly".
- Keep each `CLAUDE.md` or `AGENTS.md` under about 200 lines. Imports do not reduce context cost; they load at launch.
- No contradictions between files. If two say different things, Claude may pick either.
- Put things Claude can derive from the code (directory listings, dependency lists) in no instruction file.
- Block-level HTML comments are stripped before loading, so use them for maintainer notes and review dates.
- When Claude makes the same mistake twice, write a rule or a decision.

## Enforcement (owner to set up)

Instructions are not guarantees. For hard limits use permission rules or hooks in `.claude/settings.json`, for example: deny edits under `design/prototype/`, deny adding packages, and run `dotnet format` or the tests before a commit. Check the current hooks and permissions documentation for syntax.

## Review

- Run `/context` after changes to confirm the files you expect are loaded.
- Run `/doctor prompt-audit` now and then. It finds outdated, conflicting or broken instructions and proposes edits.
- Review `.claude/rules/`, skills and `AGENTS.md` every quarter, with the statuses in `docs/decisions.md`.
