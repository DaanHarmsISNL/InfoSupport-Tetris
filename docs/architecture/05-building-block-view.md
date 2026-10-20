# 5. Building Block View

## Level 1: Overall System

<!-- Top-level decomposition of the system into its main building blocks -->

```
                 +-----------------------------------+
   HTTP           |              Pages                 |
  request  ----->  |  Index.cshtml / Index.cshtml.cs    |
                 |  (IndexModel: OnGet, OnPostStart,  |
                 |   OnPostNewGame, OnPostHardDrop,   |
                 |   OnPostInput, OnPostRelease)      |
                 |  + _NextPiecePreview, _HoldDisplay |
                 +------------------+------------------+
                                    | reads/replaces GameSession
                                    v
                 +-----------------------------------+
                 |             Services               |
                 |  IGameSessionStore / GameSessionStore|
                 |  (ConcurrentDictionary<sessionId,   |
                 |   GameSession>, one per browser      |
                 |   session cookie)                   |
                 +------------------+------------------+
                                    | owns
                                    v
                 +-----------------------------------+
                 |              Domain                |
                 |  GameSession (aggregate root)       |
                 |   -> Playfield, ActivePiece,        |
                 |      PieceQueue/SevenBag, KickTable, |
                 |      InputState, scoring types       |
                 |  (framework-free C#)                 |
                 +-----------------------------------+

                 +-----------------------------------+
                 |             wwwroot                |
                 |  css/site.css, css/tokens/*,        |
                 |  js/site.js (keydown -> OnPostInput/|
                 |  OnPostRelease via fetch),           |
                 |  lib/ (vendored Bootstrap/jQuery)    |
                 +-----------------------------------+
                      served statically alongside Pages
```

| Building Block | Description |
| -------------- | ----------- |
| `Pages` | Razor Pages UI/HTTP layer. `IndexModel` is the single game page's code-behind: resolves/creates the session via `IGameSessionStore`, drives gravity by calling `GameSession.Tick` on every `OnGet` (auto-refresh loop) and before handling `OnPostHardDrop`/`OnPostInput`, and dispatches player commands (`PressLeft/Right`, `RotateClockwise/CounterClockwise`, `PressSoftDrop`/`ReleaseSoftDrop`, `Hold`, `HardDrop`) onto the `GameSession`. Exposes read-only projections (`ActivePieceVisibleCells`, `Preview`, `Hold`) for the `.cshtml` view and partials `_NextPiecePreview.cshtml`/`_HoldDisplay.cshtml`. |
| `Services` | `IGameSessionStore`/`GameSessionStore` — an in-memory, per-ASP.NET-Core-session-cookie store (`ConcurrentDictionary<string, GameSession>`) holding at most one in-progress `GameSession`. No persistence beyond process lifetime (NFR-007: anonymous play only); `Replace` makes Start/New-game idempotent rather than appending sessions. |
| `Domain` | The framework-free game model: `GameSession` as aggregate root, orchestrating `Playfield`, `ActivePiece`, `PieceQueue`/`SevenBag`, `KickTable`, `InputState`, and the scoring/level/game-over value types. This is the system's architectural core — see Level 2. |
| `wwwroot` | Static assets: `css/site.css` and `css/tokens/*` (design tokens) for board/HUD styling, `js/site.js` wiring keyboard events to the page's `OnPostInput`/`OnPostRelease` handlers, and vendored `lib/` (Bootstrap, jQuery, jQuery-validation) used by the scaffold layout. Served directly by ASP.NET Core static files middleware; no build step. |

## Level 2: Component Details

<!-- Decomposition of individual building blocks from level 1 -->

### Domain

`GameSession` is the aggregate root for one playthrough (`Ready` -> `Playing` -> `GameOver`, per `GameStatus` in `Enums.cs`). Nothing outside `GameSession` is reachable except through it, and it is the only type with mutator methods — `Playfield`, `ActivePiece` and the queue are otherwise manipulated only via `GameSession`'s own logic.

```
                        GameSession
                        - Status, Score, Level, LinesCleared
                        - Input: InputState
                        - LastLineClear, LastHardDropOutcome
                        - CompletedRun (on GameOver)
   +----------+----------+----------+-----------+------------+
   |          |          |          |           |            |
Playfield  ActivePiece  PieceQueue  KickTable   Scoring      HeldType /
(grid,     (falling     /SevenBag   /KickOffset (ScoreAward, HoldDisplay
40x10,     piece,       (7-bag      /KickTable  LineClear    (spec 005
buffer     rotation,    draw +      Kind        Result,      hold slot)
rows)      lock timers) upcoming)   (SRS kicks) CompletedRun)
```

- **`Playfield`** — the 10x40 grid (`ColumnCount` = 10, `BufferRowCount` = 20 hidden spawn rows above the 20 visible rows, `TotalRowCount` = 40). Pure grid operations: `IsValidPlacement` (containment + no-overlap), `DetectFullRows`, `CollapseRows`, `GetCell`/`GetVisibleCell`. Holds no piece or score state itself.
- **`ActivePiece`** — the single falling tetromino. Tracks `Position`, `Orientation`, `FallTimerElapsed`, `LockTimerElapsed`/`LockResetsUsed`/`LowestRowReached` (lock-delay state), and `SoftDropCells`/`LastKickTest`. Exposes placement queries used by `GameSession`: `OccupiedCells`, `IsGrounded`, `TryStepDown`, `TryMoveHorizontal`, `TryRotate` (kick-tested), `ComputeHardDropDistance`, `TryResetLockTimer`.
- **`PieceQueue` / `SevenBag`** — the 7-bag randomizer (`SevenBag`) feeding an upcoming-piece queue; `GameSession.Spawn`/`Hold` call `PieceQueue.Draw()`. `NextPiecePreview` is a derived, uncached projection of `PieceQueue`'s upcoming entries, recomputed on every access (`GameSession.NextPiecePreview`).
- **`KickTable` / `KickTableKind` / `KickOffset`** — SRS-style rotation kick data. `ActivePiece.TryRotate` looks up the ordered offset row for a `(fromOrientation, toOrientation)` pair via `KickTable.RowFor` and applies the first offset that yields a valid placement; the O-piece (`KickTableKind.None`) rotates in place with no kick test.
- **Scoring/game-over types** — `ScoreAward`/`ScoreSource` (one scored event), `LineClearResult`/`ClearType` (outcome of a lock's line-clear resolution), `HardDropOutcome`, `CompletedRun`/`TopOutReason` (immutable end-of-session summary, built once in `GameSession.CompleteGameOver`). `GameSession.AwardScore` is the sole mutator of `Score`.
- **`InputState` / `InputCommand` / `HorizontalDirection` / `RotationDirection`** — the session's single keyboard-state model (DAS/ARR auto-repeat, active direction, soft-drop held), advanced each `GameSession.Tick` via `Input.AdvanceAndCountRepeats` before gravity/lock timers are evaluated.
- **`Cell` / `CellPosition` / `ShapeOffset` / `TetrominoDefinition`** — small value types; `TetrominoDefinition.For(type)` supplies per-shape spawn offsets, orientation offsets and colour, used by both `ActivePiece` (occupied-cell math) and `Pages/IndexModel` (rendering colour).

`GameSession`'s own responsibilities beyond delegating to these types: `Start`/`Spawn`/`SpawnPieceOfType` (spawn placement + DR-08 free initial descent + block-out detection), `Tick` (gravity, soft drop, lock-delay accumulation and lock-out), `HardDrop`, `Hold` (stash/swap via the single `SpawnPieceOfType` path), `LockActivePiece` -> `ResolveLineClear` -> `RecomputeLevel` (the lock resolution sequence), and `CompleteGameOver` (the single path to `GameStatus.GameOver`).

### Pages

`IndexModel` (`Pages/Index.cshtml.cs`) is a thin orchestration layer: it never implements game rules itself, only (a) resolves the current `GameSession` from `IGameSessionStore` keyed by `HttpContext.Session.Id`, (b) calls `GameSession.Tick` with real elapsed wall-clock time to catch up gravity before handling any request, and (c) forwards each `InputCommand`/handler to the matching `GameSession` method. Because there is no client-side game loop, the page relies on a meta-refresh (`OnGet`) to keep ticking gravity, while `OnPostInput`/`OnPostHardDrop`/`OnPostRelease` give low-latency key handling between refreshes.

### Services

`GameSessionStore` is a minimal `ConcurrentDictionary`-backed store with two operations (`Get`, `Replace`) and no persistence or expiry — it exists solely to give each browser session cookie its own `GameSession` instance and to make "New game" a clean replace rather than an accumulating list.

## Level 3: Internal Structure (if needed)

<!-- Further decomposition only where architecturally significant -->

### Gravity / lock-delay timing state machine (`GameSession.Tick` + `ActivePiece`)

This is the one part of the domain intricate enough to warrant a dedicated walkthrough, since several independently-evolving timers interact:

```
Tick(elapsed):
  elapsed <= 0?            -> no-op (EC-10)
  clamp elapsed to MaxElapsedPerTick (100ms, DR-26/EC-2)
  UpdateLowestRowReached(piece)              // DR-17 baseline
  Input.AdvanceAndCountRepeats(clamped)       // DAS/ARR repeats resolved first (DR-43)
    -> replay TryMoveActiveDirection() per repeat
  piece.IsGrounded(Playfield)?
    yes -> LockTimerElapsed += clamped
           LockTimerElapsed >= LockDelay (500ms)? -> LockActivePiece(piece)
           return
    no  -> LockTimerElapsed = null              // DR-18: cancel, no refund
           FallTimerElapsed += clamped
           interval = SoftDropHeld ? SoftDropInterval : GravityInterval
           FallTimerElapsed >= interval && not grounded?
             -> TryStepDown(); FallTimerElapsed -= interval
                SoftDropHeld? -> SoftDropCells++, AwardScore(1, SoftDrop)
                UpdateLowestRowReached(piece)   // may replenish reset budget
```

Key invariants enforced across `GameSession` and `ActivePiece`:
- `GravityInterval` is a pure function of `Level` (hard-coded per-level millisecond table, `GravityIntervalMillisecondsByLevel`, so the curve can never drift from floating-point rounding).
- `SoftDropInterval` = `GravityInterval / InputState.SoftDropFactor` (accelerated gravity, not an independent timer).
- A move/rotation that succeeds while grounded either resets `LockTimerElapsed` (consuming one of `ActivePiece.MaxLockResets` = 15 resets, via `TryResetLockTimer`) or cancels the lock delay outright if the action ungrounds the piece (`GameSession.ApplyGroundedActionTrigger`).
- `UpdateLowestRowReached` replenishes the 15-reset budget whenever the piece reaches a new lowest row, taking precedence over the per-action reset-or-cancel logic.
- Hard drop (`GameSession.HardDrop`) bypasses this state machine entirely — it computes `ActivePiece.ComputeHardDropDistance`, discards any running lock delay without consuming/refunding the budget, and locks immediately.
