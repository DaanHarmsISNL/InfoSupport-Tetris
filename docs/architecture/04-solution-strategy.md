# 4. Solution Strategy

<!-- Summary of fundamental decisions and solution strategies that shape the architecture -->

## Technology Decisions

<!-- Key technology choices and their rationale -->

- **ASP.NET Core Razor Pages, no client-side JS framework.** The game is a single page (`Pages/Index.cshtml`/`.cshtml.cs`). Chosen over a SPA/JS framework (React, Blazor WASM, etc.) to keep the game state server-authoritative, avoid a client build toolchain entirely, and keep the whole stack in one familiar, simple technology. This is consistent with the project's nature as a training/personal project, not a production multiplayer game.
- **Server-driven game loop via meta-refresh + POST handlers, not SignalR/WebSockets.** A meta-refresh triggers a periodic `GET /`, which calls `GameSession.Tick(elapsed)` using the real wall-clock time elapsed since `Session.LastTickAt`. Low-latency player input (move/rotate/drop/hold) is submitted via small POST page handlers that return `204 No Content` and rely on the next auto-refresh to re-render the board. This was chosen over a persistent connection (SignalR/WebSockets) because it needs no real-time server infrastructure and keeps all gameplay logic in plain, testable C# rather than push-notification plumbing. **Trade-off:** there is a visible latency window between an input and the next refresh-driven render — an accepted limitation rather than an oversight (see the Risks and Technical Debt doc).
- **In-memory session store, no database.** `GameSessionStore` is a `ConcurrentDictionary<string, GameSession>` keyed by the ASP.NET Core session cookie id, registered as a singleton `IGameSessionStore` in `Program.cs`. Chosen over a persisted store (SQL/NoSQL/Redis) because NFR-007 explicitly rules out persisted identity or accounts, and a database would add operational complexity with no corresponding requirement. Trade-off: all in-progress games are lost on app restart — acceptable for a single-process, non-production deployment.

## Top-level Decomposition

<!-- How the system is broken down into components/modules -->

- **`Domain/`** — framework-free C# game logic with no dependency on ASP.NET Core types: `GameSession` (aggregate root), `Playfield`, `ActivePiece`, `PieceQueue`/`SevenBag` (7-bag randomizer), `KickTable` (rotation kicks), `NextPiecePreview`, `HoldDisplay`, `LineClearResult`, `ScoreAward`, `CompletedRun`, `TetrominoDefinition`, `InputState`, and shared `Enums`. Keeping this layer free of web framework references is what makes it independently unit-testable.
- **`Services/`** — hosting/session infrastructure that bridges the domain layer to ASP.NET Core, currently just `GameSessionStore` (an `IGameSessionStore` singleton holding one `GameSession` per browser session id).
- **`Pages/`** — the Razor Pages UI and HTTP entry points: `Index.cshtml`/`.cshtml.cs` renders the board and exposes the POST input handlers; `Shared/_Layout.cshtml` plus `_ViewImports.cshtml`/`_ViewStart.cshtml` provide shared page scaffolding.
- **`wwwroot/`** — static assets: vendored Bootstrap/jQuery/jQuery-validation under `lib/`, and the hand-built CSS design-token layer under `css/tokens/` that bridges into Bootstrap for styling.
- **`tests/infosupport-tetris.Tests/`** — xUnit test project, one test file per feature spec, exercising the `Domain/` layer in isolation from the web host.

## Approaches to Achieve Quality Goals

<!-- How key quality goals from section 1 are addressed -->

| Quality Goal | Approach |
| ------------ | -------- |
| Testability | `Domain/` is kept entirely free of ASP.NET Core types, so game rules (piece movement, rotation/kicks, line clears, scoring, leveling) can be exercised directly by xUnit tests with no web host, HTTP, or session plumbing involved. |
| Traceability | Each feature spec (`docs/specs/NNN-*.md`) defines FR-/DR-/EC- requirement and edge-case IDs; these IDs are traced directly into code comments (domain classes, page handlers, tests), so a requirement can be located in the implementation and vice versa. |
| Simplicity / learnability | Plain Razor Pages with no client-side JS framework and no build toolchain (no npm/webpack/bundler step); vendored client libraries are edited/replaced in place via LibMan rather than a package manager graph. This keeps the project approachable for a single developer working incrementally. |
| Maintainability of state handling | Session state is isolated behind the small `IGameSessionStore` interface/singleton, so the in-memory implementation could be swapped later (e.g. for a distributed cache) without touching `Domain/` or `Pages/`. |

## Organizational Decisions

<!-- Development process, delegation, team structure decisions -->

- **Spec-driven, incremental delivery.** The game is built one numbered feature spec at a time (`docs/specs/001-start-new-game.md` through `014-controls-panel.md`), each spec driving its own branch/commit(s) and an accompanying xUnit test file, rather than a big-upfront design.
- **AI-assisted development.** Implementation work is done with Claude Code as a pair-programming/authoring tool, with specs and requirement IDs acting as the shared source of truth between developer and AI across sessions.
- **Personal/training project.** There is no formal team process, ceremony, or release cadence — this reflects a single-developer training context, not a product team.
