# 2. Architecture Constraints

## Technical Constraints

<!-- Hardware, software, infrastructure, and technology constraints -->

| Constraint | Explanation |
| ---------- | ----------- |
| .NET 10 / ASP.NET Core Razor Pages | Single `Microsoft.NET.Sdk.Web` project (`infosupport-tetris.csproj`, `TargetFramework net10.0`). No SPA framework (React/Angular/Blazor WASM) is used; the UI is server-rendered Razor Pages (`.cshtml`/`.cshtml.cs` pairs). |
| No database | No database/ORM dependency exists in the project file. `Services/GameSessionStore.cs` keeps game state in an in-memory `ConcurrentDictionary`, keyed by an anonymous cookie session id (`AddDistributedMemoryCache()` + `AddSession()` in `Program.cs`). Consequence: all game state is lost on app restart/redeploy and the store does not scale across multiple instances/processes (no shared/distributed cache backing it). |
| Nullable reference types enabled | `<Nullable>enable</Nullable>` is set in both `infosupport-tetris.csproj` and `tests/infosupport-tetris.Tests/infosupport-tetris.Tests.csproj`; code is written to satisfy nullable analysis rather than suppress it. |
| Server-driven game loop, no WebSockets/SignalR | No SignalR, WebSocket, or push-based mechanism is used. `Pages/Index.cshtml` relies on a meta-refresh auto-refresh plus small synchronous POST handlers (`OnPostInput`, `OnPostHardDrop`, etc.) in `Pages/Index.cshtml.cs` to advance and read game state. |
| Vendored client-side libraries | Bootstrap and jQuery live under `wwwroot/lib/` as vendored, checked-in files, not npm/yarn-managed. `wwwroot/js/site.js` is plain JS; the design-token CSS layer (`wwwroot/css/tokens/*`) is hand-rolled on top of Bootstrap, not the output of a bundler/build step. |

## Organizational Constraints

<!-- Team structure, schedule, budget, and process constraints -->

| Constraint | Explanation |
| ---------- | ----------- |
| Solo/training project | Personal project under `DaanHarmsISNL` on GitHub, not built by a team — no separate QA, design, or ops function; one developer owns all decisions. |
| Spec-driven, incremental delivery | Functionality is added feature-by-feature following numbered specs in `docs/specs/001-*.md` through `014-*.md`, each defining coded requirements (FR-/DR-/EC-/NFR-) before implementation, rather than an upfront full design. |
| Claude Code as development tool | The codebase has been built with Claude Code as an AI coding assistant; commit history and code comments reflect spec-to-code traceability established through that workflow rather than a multi-person review process. |

## Conventions

<!-- Coding standards, documentation guidelines, naming conventions -->

| Convention | Explanation |
| ---------- | ----------- |
| Requirement-ID traceability | Requirements from `docs/specs/*.md` are coded as FR- (functional), DR- (data/domain), EC- (edge case), and NFR- (non-functional) IDs, referenced directly in code comments (e.g. `Program.cs` cites `NFR-007`) to keep implementation traceable back to the originating spec. |
| Razor Pages code-behind pairing | Each page is a `.cshtml` markup file paired with a `.cshtml.cs` code-behind class (e.g. `Pages/Index.cshtml` + `Pages/Index.cshtml.cs`); POST handlers follow the `OnPost<Action>` naming convention. |
| Domain logic isolated in `Domain/` | Core Tetris game rules live in `Domain/` as plain C# with no ASP.NET Core/framework dependencies, making them independently unit-testable. |
| Dedicated xUnit test project | Tests live in `tests/infosupport-tetris.Tests/` (xUnit, referencing the main project), excluded from the main project's compile/content items via the `tests/**` removal rules in `infosupport-tetris.csproj`. |
| `Services/` for infrastructure | Cross-cutting, framework-facing concerns (e.g. `Services/GameSessionStore.cs`) are registered as singletons/DI services in `Program.cs`, kept separate from the pure `Domain/` logic. |
