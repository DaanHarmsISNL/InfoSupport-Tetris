---
name: wiki
description: Use when the user asks to create, edit, or view a page on this project's GitHub wiki. Covers the clone/edit/push workflow, the wiki's separate git URL and default branch, and a Windows-specific /tmp path gotcha.
---

Project wiki pages live in the repo's GitHub wiki, **not** Confluence — there is no "space key" concept; GitHub wikis are scoped one-per-repo. There is also no MCP/REST API for GitHub wikis (the `github` MCP server's tools don't cover them), so pages are created with plain `git`:

- **Wiki git URL**: `https://github.com/DaanHarmsISNL/InfoSupport-Tetris.wiki.git`
- **Human URL**: https://github.com/DaanHarmsISNL/InfoSupport-Tetris/wiki
- **Default branch**: `master` (not `main`)
- **Page = file**: each page is a `.md` file at the repo root, named with hyphens for spaces (e.g. `Workshop test page` → `Workshop-test-page.md`).
- **Prerequisite**: the wiki feature must already be enabled on the repo (`has_wiki: true`) *and* at least one page must already exist — GitHub doesn't create the `.wiki.git` repo until the first page is saved via the web UI. If `git clone` of the wiki URL 404s, that's why; ask the user to create one page via the web UI first.

Workflow to add a page: `git clone` the wiki URL to a scratch dir, add/edit the `.md` file, commit, `git push origin master`. Pushing is a shared/visible action — confirm with the user before pushing, same as any other push.

**Gotcha**: on this machine, Bash's `/tmp` and the Write tool's `/tmp` resolve to different real paths (Bash → `AppData/Local/Temp`, Write tool → `C:\tmp`). When writing a file with the Write tool that a subsequent `git add`/`commit` (via Bash) needs to see, write inside the actual git working directory (or `ls`/`find` to confirm the real path) rather than assuming `/tmp` is shared between the two tools.
