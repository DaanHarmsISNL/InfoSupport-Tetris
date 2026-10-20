# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

This is an unmodified ASP.NET Core Razor Pages scaffold (`dotnet new webapp`) intended as the starting point for building a Tetris game. No game logic, tests, or custom pages exist yet — `Pages/Index.cshtml` still has the default template welcome text. `docs/architecture/*.md` are empty arc42 templates (placeholders only, no content filled in). Treat this as a greenfield project: there are no existing conventions to preserve beyond what's described below, so establish clean patterns as functionality is added.

## Repository

- **Host**: GitHub, via the `github` MCP server (`mcp__github__*` tools) — no separate "git repository" server exists in this session.
- **Owner/organization**: `DaanHarmsISNL` (a personal account, not an org)
- **Repo name**: `InfoSupport-Tetris`
- **Clone URL**: `https://github.com/DaanHarmsISNL/InfoSupport-Tetris.git`
- **Default branch**: `main`
- **Visibility**: public
- This is currently the only repo under `DaanHarmsISNL` (confirmed via `mcp__github__search_repositories` with `query: "user:DaanHarmsISNL"`) — if more are added later, re-run that search rather than assuming this is still the only match.

## Commands

- Run the app: `dotnet run` (serves on `http://localhost:5207` / `https://localhost:7257`, per `Properties/launchSettings.json`)
- Build: `dotnet build`
- Restore packages: `dotnet restore`
- No test project exists yet. When adding one, wire it up with `dotnet test`.

## Issue tracker

Issues are tracked on GitHub (not Jira/Linear) via the `github` MCP server, using the `mcp__github__issue_write` tool (method: `create`).

- **Owner**: `DaanHarmsISNL`
- **Repo**: `InfoSupport-Tetris`
- **Repo URL**: https://github.com/DaanHarmsISNL/InfoSupport-Tetris

GitHub has no separate "project key" or cloud/org ID to supply — `owner` + `repo` above are the only identifiers `issue_write` needs. The authenticated user is `DaanHarmsISNL` (confirmed via `mcp__github__get_me`).

## Wiki

Project wiki pages live in the repo's GitHub wiki (not Confluence), at `https://github.com/DaanHarmsISNL/InfoSupport-Tetris/wiki`. For the clone/edit/push workflow, the separate `.wiki.git` URL and `master` default branch, and a Windows `/tmp` path gotcha, see the `wiki` skill.

## Architecture

- **Framework**: ASP.NET Core 10.0 Razor Pages (`Microsoft.NET.Sdk.Web`), root namespace `infosupport_tetris`.
- **Entry point**: `Program.cs` — minimal hosting model, wires up `AddRazorPages()`, static assets, HTTPS redirection, and the `/Error` exception handler for non-development environments.
- **Pages**: under `Pages/`, following standard Razor Pages conventions (`.cshtml` + `.cshtml.cs` code-behind pairs). `Pages/Shared/_Layout.cshtml` is the shared layout; `_ViewImports.cshtml`/`_ViewStart.cshtml` configure shared imports and layout selection.
- **Client assets**: `wwwroot/` holds static files; `wwwroot/lib/` contains vendored Bootstrap/jQuery/jQuery-validation libraries (not npm-managed — edit in place or replace via LibMan if adding new client dependencies).
- **Architecture docs**: `docs/architecture/` follows the arc42 template (numbered `00`–`12` files). These are currently empty placeholders — fill them in as architectural decisions for the Tetris implementation are made, rather than creating new documentation files elsewhere.
