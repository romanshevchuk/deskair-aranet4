@AGENTS.md
@docs/00-index.md
@docs/engineering/csharp-instructions.md

## Claude Code specifics

- Area rules: `.claude/rules/*.md` have `paths` frontmatter and load when you read, edit or create a matching file.
- Skills (load when relevant or when invoked): `add-a-control`, `add-a-setting`, `add-a-translation`, `add-a-sensor`.
- Slash commands: `/implement-feature <ID>`, `/review-against-design <area>`, `/update-docs`.
- Check what is loaded with `/context` (Memory files). Audit the instruction files with `/doctor prompt-audit`.
- `csharp-instructions.md` may be empty. Then follow standard .NET conventions and ask before inventing project-wide rules.

<!-- Maintainer note: keep this file and AGENTS.md under 200 lines combined. See docs/guides/maintaining-agent-instructions.md. -->
