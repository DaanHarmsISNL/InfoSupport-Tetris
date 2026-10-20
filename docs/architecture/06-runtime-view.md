# 6. Runtime View

<!-- Key runtime scenarios showing how building blocks interact -->

## Scenario 1: Auto-refresh gravity tick

There is no client-side game-loop framework. While `Status == Playing`, `Pages/Index.cshtml`
emits `<meta http-equiv="refresh" content="0.25">`, so the browser itself re-issues a full
`GET /` roughly 4 times a second. `IndexModel.OnGet` uses this as the sole gravity/lock-delay
clock: it computes real wall-clock elapsed time since `Session.LastTickAt` and calls
`GameSession.Tick(elapsed)` before the page renders, so every refresh both advances and displays
state. The spec-tested logic lives entirely in `GameSession.Tick`, which is exercised
deterministically in tests via simulated deltas rather than real time.

```
Browser              Handler (OnGet)           GameSession            Store
  |                        |                         |                   |
  |-- GET / (meta-refresh) ->|                       |                   |
  |                        |-- Get(SessionId) ------------------------->|
  |                        |<----------------- existing GameSession -----|
  |                        |                         |                   |
  |                        |-- now = UtcNow          |                   |
  |                        |-- elapsed = now - Session.LastTickAt        |
  |                        |-- Tick(elapsed) ------->|                   |
  |                        |                         |-- advance fall/lock
  |                        |                         |   timers, lock/spawn,
  |                        |                         |   line clears, etc.
  |                        |<-- (session mutated) ---|                   |
  |                        |-- Session.LastTickAt = now                  |
  |<-- 200 HTML (re-render with new board state) ----|                   |
  |                        |                         |                   |
  |-- (0.25s later) GET / -->|  ... cycle repeats ... |                   |
```

This makes the meta-refresh the actual render loop: it is both the input (driving gravity
forward by the correct amount of real time, however late the refresh arrives) and the output
(the only place the new board, score, level and active piece are drawn).

## Scenario 2: Low-latency input between refreshes

Gravity alone only advances once per ~250ms refresh, which is too coarse for responsive controls.
`wwwroot/js/site.js`'s inline script (in `Index.cshtml`) listens for `keydown`/`keyup` and posts
edge-triggered (`event.repeat` is ignored — DR-31/DR-34, OS autorepeat is never trusted; the game
times its own DAS/ARR) `fetch` calls to `OnPostInput`/`OnPostRelease` immediately, independent of
the refresh cycle. `OnPostInput` first catches gravity up to "now" (DR-43: elapsed-time gravity
before the command is applied, so the command acts on fully up-to-date state), then dispatches
exactly one command via switch, then returns `204 No Content`. No HTML is sent back — the
response is deliberately invisible to the player until the next scheduled meta-refresh paints it.

```
Browser (site.js)         Handler (OnPostInput)      GameSession            Store
  |                              |                        |                   |
  |-- keydown (e.g. ArrowLeft) --|                        |                   |
  |   (ignore if e.repeat)       |                        |                   |
  |-- fetch POST ?handler=Input  |                        |                   |
  |   &command=MoveLeft -------->|                        |                   |
  |                              |-- Get(SessionId) ----------------------->|
  |                              |<----------- existing GameSession ---------|
  |                              |-- now = UtcNow                            |
  |                              |-- Tick(now - LastTickAt) ->|              |
  |                              |<-- (gravity caught up) ----|              |
  |                              |-- LastTickAt = now                        |
  |                              |-- PressLeft() ----------->|              |
  |                              |<-- (position updated) ----|              |
  |<-- 204 No Content -----------|                        |                   |
  |  (no visible change yet)     |                        |                   |
  |                              |                        |                   |
  |-- (next meta-refresh) GET / -->  [Scenario 1 runs, renders new position]
```

The same pattern applies to `OnPostRelease` (key-up / window-blur releasing `MoveLeft`,
`MoveRight`, `SoftDropEnd`) and to `OnPostHardDrop` (bound to `Space` via a hidden auto-submitted
form, also gated by an edge-triggered `armed` flag in the page script) — both tick gravity up to
"now" first and both return 204/redirect without themselves drawing anything new; the player only
*sees* the result on the next auto-refresh.

### Variant: Hold (DR-72 ordering exception)

`Hold` is bound client-side the same way (`C` / `Shift`, key-down edge only) and posts to the same
`OnPostInput` endpoint, but the handler special-cases it: `InputCommand.Hold` resolves *before* the
gravity catch-up tick, the only command for which this is true. Per docs/specs/005-hold-piece.md
DR-72, a hold must beat an expiring lock delay (SC-014) — if gravity ticked first, the lock delay
could expire and lock the piece in the same step before the hold had a chance to rescue it. So the
handler calls `session.Hold()` against the state as it stood at the end of the *previous* tick,
then sets `LastTickAt = UtcNow` (effectively deferring that interval's gravity to the next request)
and returns 204 immediately, skipping the normal tick-then-dispatch path entirely.

```
Browser (site.js)         Handler (OnPostInput)      GameSession
  |-- keydown (KeyC/Shift) ------|                        |
  |-- fetch POST ?command=Hold ->|                        |
  |                              |-- Get(SessionId)       |
  |                              |-- if Playing:           |
  |                              |     Hold() ----------->|  (swap/stash active piece,
  |                              |                        |   BEFORE any gravity tick)
  |                              |<-- (hold applied) ------|
  |                              |-- LastTickAt = UtcNow  |
  |<-- 204 No Content -----------|                        |
```

## Error/Edge Case Scenarios

- **No session on input/release**: `OnPostInput` and `OnPostRelease` both call `_store.Get(SessionId)`
  first and return `NotFound()` (404) if it is `null` — e.g. the session cookie was never
  established or has expired. The client-side `fetch` calls do not check the response status, so
  today this failure is silent to the player; the next meta-refresh `GET /` will still succeed
  because `OnGet` creates a fresh `GameSession` whenever the store has none (FR-006), which
  effectively self-heals the *page* but silently drops whatever input triggered the 404.
- **Hard drop / commands with no active piece or wrong status**: `OnPostHardDrop` itself only
  calls `Tick`/`HardDrop` when `session.Status == GameStatus.Playing`; outside that it's a no-op
  and the handler just redirects. Inside `GameSession.HardDrop`, a request with no active piece
  is also discarded by the method itself (per its own code comment) rather than by the handler —
  so the guard is enforced twice, once coarsely by status in the handler and once precisely inside
  the domain method.
- **Commands posted outside `Playing` (e.g. session is `Ready` or `GameOver`)**: `OnPostInput`
  only ticks gravity and only runs `Hold()` when `Status == Playing`; the switch over
  `MoveLeft`/`MoveRight`/rotate/soft-drop still executes unconditionally against whatever
  `GameSession` returns from the store, so those calls rely entirely on `GameSession`'s own
  internal gating (DR-28) to be no-ops when not playing — the page-level script only renders the
  key bindings at all while `Status == Playing`, so in practice the browser does not normally emit
  these POSTs outside that state, but nothing stops a non-browser client from doing so.
- **Session cookie expiring mid-game**: not explicitly handled. If the session cookie expires or
  is cleared while `Status == Playing`, the next `OnGet` finds no session in the store and creates
  a brand-new `Ready` `GameSession` (same path as a first-ever visit), silently discarding the
  in-progress run with no warning, no game-over summary and no score preserved. This is a known
  gap rather than a designed behaviour.
- **Blur releasing held keys (EC-7/EC-8)**: not a failure mode but a deliberate safety net — on
  `window.blur`, the script posts `Release` for any of `MoveLeft`/`MoveRight`/`SoftDropEnd` still
  held, and separately re-arms the hard-drop `Space` trigger, so losing focus (e.g. alt-tab) can
  never leave an input "stuck" pressed indefinitely.
