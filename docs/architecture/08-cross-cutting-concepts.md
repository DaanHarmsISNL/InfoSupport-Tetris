# 8. Cross-cutting Concepts

## Domain Model

The domain model lives entirely under `Domain/` and is deliberately framework-free (no ASP.NET Core
types appear in it). `GameSession` is the aggregate root for one playthrough, from the start action
to game over; everything else is reached through it:

- **Status**: a `GameStatus` enum (`Ready` / `Playing` / `GameOver`) driving every gate in the
  session's public methods.
- **Playfield**: the grid of locked cells, including hidden buffer rows above the 20 visible rows.
- **ActivePiece**: the currently falling tetromino (null before first spawn, between lock and
  spawn, and after game over); its shape/orientation comes from `TetrominoDefinition` and
  `ShapeOffset`, rotation kicks from `KickTable`/`KickOffset`/`KickTableKind`.
- **PieceQueue** / **SevenBag**: the 7-bag randomizer feeding upcoming pieces; `NextPiecePreview` is
  a read-only projection recomputed on every access (never cached) so it can never drift from the
  queue.
- **Hold slot**: `HeldType` (nullable) plus `HoldUsedThisPiece`, with `HoldAvailable` derived rather
  than stored so the Hold action and its on-screen display can never disagree.
- **Score/level/lines**: `Score`, `LinesCleared`, and `Level` (derived solely from `LinesCleared`,
  capped at `LevelCap` = 15) plus `LastLineClear`/`CompletedRun`/`ScoreAward`/`LineClearResult` for
  the most recent lock's scoring outcome.
- **Timers**: gravity and lock-delay are driven by `GameSession.Tick(TimeSpan)`, called with the
  real elapsed wall-clock time by the Razor Pages layer, and by a hard-coded, per-level gravity
  interval table (`GravityIntervalMillisecondsByLevel`) rather than a runtime formula, so the speed
  curve can never drift across platforms.
- **Game-over / leaderboard**: `EndedAt`, plus `PlayerName`, `LeaderboardEntry`, `ScoreSubmission`,
  and `SubmissionFailure` for the end-of-game score submission flow.

`InputState` and `InputCommand` model the single keyboard state per session (left/right/soft-drop
held, rotate, hard drop, hold), and `GameSession` itself gates every mutator by `Status` so invalid
transitions are simply no-ops rather than exceptions (see Error Handling below).

## Security

There is no authentication or authorization: the game has no accounts, sign-in, or persisted
player identity (NFR-007). Each browser is identified only by the anonymous ASP.NET Core session
cookie (`AddDistributedMemoryCache()` + `AddSession()` in `Program.cs`), which is used purely as a
dictionary key into the in-memory `GameSessionStore` — it carries no claims and is never validated
against anything.

`IndexModel` (`Pages/Index.cshtml.cs`) and `ErrorModel` (`Pages/Error.cshtml.cs`) are both annotated
with `[IgnoreAntiforgeryToken]`. This is a deliberate, known trade-off rather than an oversight: the
page has no JS framework, so gameplay actions (movement, rotation, soft/hard drop, hold) are posted
very frequently by small dedicated forms/fetches from an edge-triggered keydown script, and wiring
up per-request antiforgery tokens for that volume of rapid, low-stakes POSTs was judged not worth
the complexity for a game with no accounts, no persisted state, and no cross-site-sensitive action
to protect. The practical consequence is that every mutating handler on `IndexModel`
(`OnPostStart`, `OnPostNewGame`, `OnPostHardDrop`, `OnPostInput`, `OnPostRelease`) is reachable via a
plain cross-site POST carrying only the ambient session cookie — a CSRF-style request could drive
someone else's in-progress game (move pieces, hard-drop, start a new game). Given there is no
account takeover, no persisted data, and no stakes beyond a single anonymous play session, this is
accepted for now; it should be revisited if a leaderboard identity or any persisted-per-player data
is added later, since at that point a forged request could have a lasting effect.

## Persistence

There is no database, file storage, or ORM of any kind. `Services/GameSessionStore.cs` is a
singleton (`AddSingleton<IGameSessionStore, GameSessionStore>()`) wrapping a plain
`ConcurrentDictionary<string, GameSession>` keyed by the session cookie id, holding at most one
in-progress `GameSession` per browser session. `Replace` always overwrites the dictionary entry
(never appends), so repeated "Start"/"New game" actions cannot leak sessions. All state is lost on
app restart — this is intentional for a stateless, account-free game, not a gap to fill.

## Logging and Monitoring

No structured logging, metrics, or observability infrastructure has been added beyond the ASP.NET
Core default (`appsettings.json`'s standard `Logging:LogLevel` block — `Default: Information`,
`Microsoft.AspNetCore: Warning` — writing to the console via the default host logging provider).
There is no Application Insights, Serilog, OpenTelemetry, or custom metrics/tracing. This is normal
and acceptable for the project's current stage; add an observability approach if/when the game is
deployed somewhere real traffic needs to be monitored.

## Error Handling

Two distinct strategies are used, deliberately kept separate:

- **Invalid game-state transitions are silent no-ops, not exceptions.** For example, `HardDrop()`
  (and the other gameplay mutators) do nothing if called outside `GameStatus.Playing` or with no
  active piece — callers such as `IndexModel.OnPostHardDrop` don't need to catch anything; a
  stray/late POST (e.g. after game over, or a double-submit) simply has no effect. This keeps the
  domain model forgiving of the page's coarse, auto-refresh-driven request timing (DR-50).
- **Unhandled exceptions** use the standard ASP.NET Core Razor Pages pattern: in non-Development
  environments, `Program.cs` registers `app.UseExceptionHandler("/Error")` (plus `UseHsts()`), which
  re-executes the request against `Pages/Error.cshtml` / `ErrorModel`. `ErrorModel` is marked
  `[ResponseCache(Duration = 0, ..., NoStore = true)]` so error pages are never cached, and
  `[IgnoreAntiforgeryToken]` for the same reason as `IndexModel` — it must be reachable regardless
  of how the original request arrived. In Development, the framework's built-in developer exception
  page is used instead (no custom override). There is no custom exception hierarchy; this is scaffold
  behavior, unmodified.

## Configuration

Configuration is plain ASP.NET Core defaults: `appsettings.json` holds the base logging
configuration shown above, with `appsettings.Development.json` only tightening
`Microsoft.AspNetCore.*` logging to `Warning` for local development (no secrets, connection
strings, or feature flags are configured anywhere). There is no environment-specific behavior
beyond the standard `IsDevelopment()` branch in `Program.cs` (exception handler/HSTS for
non-Development only). No `appsettings.Production.json` or other environment overlay exists yet.

## Testing

Testing is unit-level only, against the domain model in isolation — there is no integration or
end-to-end test harness (no `WebApplicationFactory`, no reference to `Pages` types) in the test
project. All test files live under `tests/infosupport-tetris.Tests/` and exercise `Domain/*`
directly:

- `GameSessionStartTests.cs`
- `PieceSequenceTests.cs`
- `GravityTests.cs`
- `LockDelayTests.cs`
- `GameOverTests.cs`
- `HardDropTests.cs`
- `HoldPieceTests.cs`
- `LevelsAndSpeedTests.cs`
- `LineClearAndScoringTests.cs`
- `NextPiecePreviewTests.cs`

Gravity/lock-delay/level timing is tested via `GameSession.Tick(TimeSpan)` with simulated deltas
rather than real elapsed time, which is exactly what lets this suite run deterministically and
headlessly (see the `GameSession.Tick` usage notes above) without needing a running web host.

## Build and Deployment

There is no CI/CD pipeline yet — `.github/workflows/` does not exist in this repository. Builds and
tests are run locally only, via `dotnet build`, `dotnet restore`, and `dotnet test`
(`dotnet run` to serve the app locally per `Properties/launchSettings.json`). No deployment target,
container image, or release process has been established. This should be added once the project
needs automated verification on push/PR or an actual hosting target.
