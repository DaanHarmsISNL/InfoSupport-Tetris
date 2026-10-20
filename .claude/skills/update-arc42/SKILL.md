---
name: update-arc42
description: Update the arc42 architecture documentation in docs/architecture/ to reflect the current state of the codebase. Use when the user asks to update, refresh, sync, or regenerate the architecture docs, or after significant code changes that should be reflected in the arc42 documentation.
---

# Update arc42 documentation

Updates each numbered file in `docs/architecture/` (the arc42 template, `00-table-of-contents.md` through `12-glossary.md`) so it accurately reflects the current codebase. Each file is updated independently and in parallel by its own subagent, since the files are largely self-contained sections of the arc42 template.

## Steps

1. **Survey the codebase first**, in the main context (not delegated), so you can brief each subagent with concrete, current facts rather than letting every subagent re-discover the whole repo from scratch:
   - List `docs/architecture/*.md` and read each file's current content.
   - Identify the overall project structure: framework, entry point, `Pages/` structure, `wwwroot/` assets, any game logic / domain code added since the scaffold, tests, dependencies (check `.csproj`), build/run commands.
   - Check recent git history (`git log --oneline -20`) to understand what has changed recently, since that's often what's most out of date in the docs.
   - Note any CLAUDE.md or other project-level instructions relevant to architecture (e.g. repo identity, hosting, conventions).

2. **Spawn one subagent per arc42 file, in parallel** (single message, multiple Agent tool calls). Each subagent should:
   - Be told explicitly it is writing/updating ONE specific file (give the exact path).
   - Receive the current content of that file.
   - Receive a summary of the relevant codebase findings from step 1 (don't make it re-derive everything — pass along what's concrete and useful, but let it also read relevant source files itself to verify details specific to its section).
   - Be told the arc42 section's purpose, so it only fills in content relevant to that section's scope and doesn't duplicate content that belongs in other sections. The twelve arc42 sections and their scope:
     - `00-table-of-contents.md` — overview/index of the arc42 doc set
     - `01-introduction-and-goals.md` — requirements overview, quality goals, stakeholders
     - `02-architecture-constraints.md` — technical/organizational constraints (e.g. ASP.NET Core Razor Pages, .NET version, hosting)
     - `03-system-scope-and-context.md` — business and technical context, external interfaces
     - `04-solution-strategy.md` — fundamental decisions and solution approaches
     - `05-building-block-view.md` — static decomposition (Pages, code-behind, wwwroot, any game/domain modules)
     - `06-runtime-view.md` — key runtime scenarios (e.g. a game loop, a request/page lifecycle)
     - `07-deployment-view.md` — infrastructure, hosting, environments
     - `08-cross-cutting-concepts.md` — patterns used throughout (e.g. state management, rendering approach, validation)
     - `09-architecture-decisions.md` — ADRs / significant decisions and rationale
     - `10-quality-requirements.md` — quality tree and concrete quality scenarios
     - `11-risks-and-technical-debt.md` — known risks, technical debt, open issues
     - `12-glossary.md` — important terms (domain and technical)
   - Be instructed to preserve the arc42 structure/headings of the file, replace placeholder text with real content grounded in the actual code, keep it concise, and not invent details not supported by the codebase (it's fine for a section to stay mostly a placeholder if the codebase genuinely has nothing yet for that topic — don't fabricate).
   - Be instructed to edit the file in place (it already exists).

3. **After all subagents complete**, do a quick pass yourself:
   - Skim the diffs (`git diff docs/architecture/`) for obvious inconsistencies between files (e.g. contradicting descriptions of the same component) and fix any you find directly.
   - Report a concise summary of what was updated in each file.

## Notes

- Run subagents in parallel for speed — the twelve files are independent edits with no ordering dependency.
- Don't delegate the initial codebase survey; doing it once in the main context avoids twelve redundant explorations and lets you give each subagent sharper, more targeted instructions.
- This is a documentation-only task — subagents should not modify any code, only the markdown files in `docs/architecture/`.
