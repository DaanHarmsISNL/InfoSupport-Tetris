# 3. System Scope and Context

## Business Context

<!-- Communication partners and the data exchanged with the system from a business perspective -->

```
+------------------+                             +------------------------+
|                   |  start / new game,         |                          |
|  Player (Browser) | <------------------------> |     Tetris Web App       |
|   (anonymous,     |  move / rotate / hold /    |  (single-player Tetris)  |
|    no account)    |  soft-drop / hard-drop     |                          |
|                   |                             |                          |
|                   |  ----------------------->  |                          |
|                   |  rendered board, score,     |                          |
|                   |  level, lines cleared,      |                          |
|                   |  game-over summary          |                          |
+------------------+                             +------------------------+
```

There is exactly one communication partner from a business perspective: the anonymous player using a web browser. No accounts, sign-up, or persisted identity exist (NFR-007, spec 001) — there is no backend operator, admin, or external business system in scope.

| Communication Partner | Inputs | Outputs |
| --------------------- | ------ | ------- |
| Player (Browser)      | Start game / New game (restart, no confirmation); movement (left/right), rotation (clockwise/counter-clockwise), soft-drop (press/release), hold, hard-drop key presses | Rendered playfield (current and active piece), next-piece preview, hold slot, current score, level, lines cleared, and — on game over — a frozen final summary (score, lines, level, pieces spawned, reason for game over) |

*Partially implemented:* spec 009 (submit score) has backend support — `OnPostSubmitScore` (a JSON endpoint) plus `Domain/LeaderboardEntry.cs`, `Domain/PlayerName.cs`, `Domain/ScoreSubmission.cs`, and `Services/ScoreSubmissionService.cs`/`ILeaderboardStore` — but there is no player-reachable UI page for it yet. Specs 010 (view leaderboard) and 011 (highlight my run) have no corresponding code in `Domain/`, `Pages/`, or `Services/` at all. None of the three specs are part of the current *user-facing* system scope.

## Technical Context

<!-- Technical interfaces (channels, protocols, hardware) mapping to business I/O -->

```
                         HTTPS (Kestrel / ASP.NET Core 10.0 Razor Pages)
Browser  <==================================================================>  Tetris Web App
  |                                                                                   |
  |-- GET / (meta-refresh, every 0.25s while Playing) ----------------------------->  |
  |     drives gravity/lock server-side via GameSession.Tick, re-renders full page    |
  |<-- full HTML page (board, score, level, previews) ------------------------------  |
  |                                                                                   |
  |-- POST /?handler=Input&command=... (fetch, keydown/keyup) --------------------->  |
  |     move / rotate / hold / soft-drop; returns 204 No Content                     |
  |-- POST /?handler=Release&command=... (fetch, keyup / window blur) ------------>  |
  |     releases a held direction/soft-drop; returns 204 No Content                  |
  |-- POST /?handler=Start / NewGame (form submit) -------------------------------->  |
  |     redirects to GET / afterwards                                                |
  |-- POST /?handler=HardDrop (form submit, Space key) ---------------------------->  |
  |     redirects to GET / afterwards                                                |
  |-- POST /?handler=SubmitScore (fetch, JSON body: RunId/PlayerName/Score/...) --->  |
  |     returns JSON { Success, EntryId, PlayerName, FailureReason, FailureMessage } |
  |                                                                                   |
  |<== Set-Cookie: session id (ASP.NET Core session), read on every request ========  |
```

No WebSockets/SignalR, no third-party service calls, no external database. `OnPostSubmitScore` is the one JSON (not plain 204/redirect) endpoint, backed by `Services/ScoreSubmissionService.cs`/`ILeaderboardStore`. All other game state lives in-process in `Services/GameSessionStore.cs` (`ConcurrentDictionary<string, GameSession>`), keyed by the session cookie, and is lost on app restart — check whether `ILeaderboardStore`'s implementation is similarly in-memory-only if that matters for a given discussion.

| Technical Interface | Description |
| -------------------- | ------------ |
| HTTP(S) via Razor Pages (`Pages/Index.cshtml` / `Index.cshtml.cs`) | The only technical channel. Standard request/response over HTTPS, server-rendered HTML, no client-side framework. |
| Auto-refresh polling loop (`<meta http-equiv="refresh" content="0.25">`) | While `Status == Playing`, the browser re-issues `GET /` every 0.25s; `IndexModel.OnGet` advances gravity/lock delay via `GameSession.Tick` based on real elapsed wall-clock time, then re-renders the full board. |
| `OnPostInput` (`POST /?handler=Input&command=...`) | Small client-side script (`wwwroot/js/site.js` is otherwise a stock template stub; the actual key-capture script is inline in `Index.cshtml`) posts on `keydown` for bound keys (arrows, X/Z, Ctrl, C/Shift for hold); applies one `InputCommand`, ticks gravity first, returns `204 No Content` — the next auto-refresh renders the result. |
| `OnPostRelease` (`POST /?handler=Release&command=...`) | Posted on `keyup` (or `window.blur`, to release everything) for movement/soft-drop keys; returns `204 No Content`. |
| `OnPostStart` / `OnPostNewGame` (`POST /?handler=Start` / `=NewGame`) | Full form submits (Start button on the Ready screen; New game / "Play again" button, also bound to the `R` key on game over); each replaces the session with a fresh `GameSession` and redirects back to `GET /`. |
| `OnPostHardDrop` (`POST /?handler=HardDrop`) | Submitted by a hidden form via an edge-triggered `Space` keydown listener; ticks gravity up to now, then applies the hard drop, and redirects back to `GET /`. |
| `OnPostSubmitScore` (`POST /?handler=SubmitScore`, JSON body/response) | Backend-only support for spec 009 (no UI page submits to it yet): accepts a `RunId`/`PlayerName`/`Score`/`LinesCleared`/`Level` JSON body and returns a JSON result via `Services/ScoreSubmissionService.cs` and `ILeaderboardStore`. |
| Session cookie (ASP.NET Core session, `AddDistributedMemoryCache()` + `AddSession()` in `Program.cs`) | The sole identity mechanism — `HttpContext.Session.Id` keys the player's `GameSession` in the in-memory `GameSessionStore`. No OAuth, no external identity provider, no persisted account. |
