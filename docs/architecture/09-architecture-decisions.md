# 9. Architecture Decisions

<!-- Important, expensive, large-scale, or risky architecture decisions.
     Individual ADRs are stored in docs/architecture/decisions/ -->

## Decision Log

| ID       | Decision | Status | Date |
| -------- | -------- | ------ | ---- |
| ADR-001  | Drive the game loop server-side via meta-refresh polling plus small POST action handlers (`Pages/Index.cshtml.cs`): `OnGet` advances gravity based on elapsed wall-clock time, and input handlers (e.g. `OnPostInput`) return `204 No Content` and rely on the next auto-refresh to render results. No JavaScript framework or WebSockets/SignalR are used. | Accepted | spec 002 |
| ADR-002  | Keep game state in-memory and session-keyed (`Services/GameSessionStore.cs`, a `ConcurrentDictionary` keyed by the ASP.NET Core session cookie id) instead of a database. Driven by NFR-007 (no accounts / no persisted identity) in spec 001. | Accepted | spec 001 |
| ADR-003  | Keep the `Domain/*.cs` layer framework-free (no ASP.NET Core references) so it can be unit tested in isolation (`tests/infosupport-tetris.Tests/`); domain code comments cross-reference FR-/DR-/EC- requirement IDs back to `docs/specs/*.md` for traceability. | Accepted | n/a |
| ADR-004  | Deliver functionality incrementally via one numbered feature spec per increment (`docs/specs/001-start-new-game.md` .. `014-controls-panel.md`), each implemented and tested before starting the next. | Accepted | n/a |
| ADR-005  | Disable antiforgery validation on the game page (`[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]` on `IndexModel` in `Pages/Index.cshtml.cs`) to simplify the polling/POST-handler flow. Accepted for now given the no-accounts, non-persistent, single-player nature of the app (see NFR-007 / ADR-002); flagged as a known risk — see section 11 (Risks and Technical Debt). | Accepted (flagged risk) | n/a |

See [decisions/](decisions/) for full Architecture Decision Records.
