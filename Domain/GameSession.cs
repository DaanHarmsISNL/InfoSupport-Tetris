using System.Linq;

namespace infosupport_tetris.Domain;

/// <summary>
/// One playthrough, from the start action to game over. The aggregate root for the Play Tetris
/// domain model: everything else (Playfield, ActivePiece, PieceQueue) is reached through it.
/// See spec §5.1. Only FR-001/FR-002/FR-005 (US-001) are implemented here — spawning, gravity,
/// locking and game-over detection belong to later stories and are left as no-ops/stubs.
/// </summary>
public sealed class GameSession
{
    public Guid Id { get; } = Guid.NewGuid();

    public GameStatus Status { get; private set; } = GameStatus.Ready;

    public Playfield Playfield { get; } = new();

    /// <summary>
    /// Null before the first spawn, between lock and spawn, and after game over. US-001 never
    /// spawns a piece, so this stays null for the whole of this spec's scope (spawning is
    /// story 002's responsibility).
    /// </summary>
    public ActivePiece? ActivePiece { get; set; }

    public PieceQueue PieceQueue { get; }

    /// <summary>
    /// docs/specs/004-next-piece-preview.md: a derived, read-only projection of
    /// <see cref="PieceQueue"/>`.Upcoming[0]` (DR-52/DR-53). Recomputed on every access — never
    /// cached — so it always reflects the current <see cref="Status"/> and queue state (DR-54,
    /// EC-11).
    /// </summary>
    public NextPiecePreview NextPiecePreview => NextPiecePreview.For(this);

    /// <summary>
    /// docs/specs/005-hold-piece.md §5.1: the single stashed tetromino type. Null on a fresh
    /// session (DR-74) and never holds a position, orientation or timer (DR-64).
    /// </summary>
    public TetrominoType? HeldType { get; private set; }

    /// <summary>
    /// docs/specs/005-hold-piece.md §5.1: whether hold has already been accepted for the current
    /// <see cref="ActivePiece"/>. Set true on an accepted hold (DR-68); cleared only when a piece
    /// locks (DR-69) — never on a spawn.
    /// </summary>
    public bool HoldUsedThisPiece { get; private set; }

    /// <summary>
    /// docs/specs/005-hold-piece.md §5.1: derived, never stored. True exactly when a hold press
    /// would be accepted — the single condition both <see cref="Hold"/> (FR-013) and the hold
    /// display (FR-018) read, so the key's behaviour and the display can never disagree.
    /// </summary>
    public bool HoldAvailable => Status == GameStatus.Playing && ActivePiece is not null && !HoldUsedThisPiece;

    /// <summary>Spec 002 §5.1/§5.2: the session's single keyboard state, driving its ActivePiece.</summary>
    public InputState Input { get; } = new();

    /// <summary>
    /// docs/specs/007-levels-and-speed.md DR-94: the highest attainable <see cref="Level"/>. Not
    /// configurable.
    /// </summary>
    public const int LevelCap = 15;

    /// <summary>
    /// docs/specs/007-levels-and-speed.md §5.3: the normative table of effective gravity
    /// intervals in whole milliseconds, index 0 = level 1 .. index 14 = level 15. Each value is
    /// <c>(0.8 - ((level - 1) * 0.007)) ^ (level - 1)</c> seconds (DR-92), rounded to the nearest
    /// whole millisecond (DR-93). Hard-coded rather than computed at runtime so the curve can
    /// never drift from the normative table due to platform floating-point differences (DR-93,
    /// EC-13).
    /// </summary>
    private static readonly int[] GravityIntervalMillisecondsByLevel =
    {
        1000, 793, 618, 473, 355, 262, 190, 135, 94, 64, 43, 28, 18, 11, 7
    };

    /// <summary>
    /// docs/specs/007-levels-and-speed.md DR-90: derived solely from <see cref="LinesCleared"/>
    /// (story 006's), never written directly except by this derivation. Starts at 1 (DR-23/FR-002)
    /// and never decreases within a session (DR-91).
    /// </summary>
    public int Level { get; private set; } = 1;

    /// <summary>
    /// docs/specs/006-line-clears-and-scoring.md DR-83: the sum of every <see cref="ScoreAward"/>
    /// made this session. Non-negative, non-decreasing; held as a 64-bit integer so overflow
    /// cannot arise under any reachable sequence of play.
    /// </summary>
    public long Score { get; private set; }

    /// <summary>
    /// Spec 006 DR-82: cumulative count of rows cleared this session. The single source of truth
    /// for stories 007 (level) and 008 (game over screen) — neither maintains its own count.
    /// </summary>
    public int LinesCleared { get; private set; }

    /// <summary>
    /// Spec 006 §5.1: the outcome of the most recently resolved lock, including a
    /// <see cref="Domain.ClearType.None"/> result. Null until the first lock of the session.
    /// </summary>
    public LineClearResult? LastLineClear { get; private set; }

    public int PiecesSpawned { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>
    /// docs/specs/008-game-over.md §5.1 (GameSession extension): null while Ready or Playing; set
    /// once, at the move to GameOver, and immutable thereafter (complements <see cref="StartedAt"/>).
    /// Derived from <see cref="StartedAt"/> plus accumulated simulated play time, never real
    /// wall-clock time, so it is assertable headlessly (NFR-007) without relying on the UI's
    /// real-time <see cref="Tick"/> caller.
    /// </summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>
    /// docs/specs/008-game-over.md DR-103: null unless and until the session tops out; set exactly
    /// once, at the transition to GameOver, from the frozen session's values. Never set for a
    /// session discarded mid-play (DR-103/SC-017).
    /// </summary>
    public CompletedRun? CompletedRun { get; private set; }

    /// <summary>
    /// Bookkeeping for <see cref="EndedAt"/> (DR-103/NFR-007): the sum of every elapsed
    /// <see cref="Tick"/> delta (post-clamp, DR-26) since <see cref="Start"/>, so the moment of
    /// game over can be expressed in simulated time without reading the real wall clock.
    /// </summary>
    private TimeSpan _elapsedPlayTime = TimeSpan.Zero;

    /// <summary>
    /// Not part of the spec's domain model — bookkeeping for the UI wiring (<c>Pages/Index</c>),
    /// which calls <see cref="Tick"/> with the real elapsed wall-clock time between page
    /// requests. Tests should call <see cref="Tick"/> directly with simulated deltas instead of
    /// relying on this.
    /// </summary>
    public DateTimeOffset? LastTickAt { get; set; }

    /// <summary>
    /// docs/specs/007-levels-and-speed.md DR-14/DR-92: time per one-cell descent, derived from
    /// <see cref="Level"/> via the Tetris Guideline curve (§5.3 table). 1.000 s at level 1,
    /// 7 ms at level 15 (DR-94). A pure function of <see cref="Level"/> alone -- re-derived
    /// automatically whenever <see cref="Level"/> changes, and at no other time (DR-92).
    /// </summary>
    public TimeSpan GravityInterval => TimeSpan.FromMilliseconds(GravityIntervalMillisecondsByLevel[Level - 1]);

    /// <summary>
    /// DR-26/EC-2: the maximum elapsed time a single <see cref="Tick"/> call consumes, so a
    /// stalled/backgrounded tab cannot produce a multi-cell catch-up burst.
    /// </summary>
    public static readonly TimeSpan MaxElapsedPerTick = TimeSpan.FromMilliseconds(100);

    /// <summary>DR-16: a grounded piece locks after 0.500 second of continuous grounded time.</summary>
    public static readonly TimeSpan LockDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>DR-39: time per cell while soft drop is held — gravityInterval / 20 (50 ms at level 1).</summary>
    public TimeSpan SoftDropInterval => TimeSpan.FromTicks(GravityInterval.Ticks / InputState.SoftDropFactor);

    public GameSession(int? pieceQueueSeed = null)
    {
        PieceQueue = new PieceQueue(pieceQueueSeed);
    }

    /// <summary>
    /// FR-001/FR-002: Ready -> Playing, with a clean Playfield, level 1, score 0, no active
    /// piece, and a freshly seeded piece queue (DR-23). No data entry is required or possible.
    /// </summary>
    public void Start(DateTimeOffset now)
    {
        if (Status != GameStatus.Ready)
        {
            return;
        }

        Status = GameStatus.Playing;
        StartedAt = now;
        LastTickAt = now;
        Input.Reset(); // DR-28: a fresh InputState for the new session.

        // US-002 (FR-010/011): there is no gravity loop yet (story 003), so with nothing else to
        // drive it, the first piece spawns here, immediately on Start. Story 003/004 should call
        // Spawn() again after a lock resolves (DR-20) instead of relying on this call site alone.
        Spawn();
    }

    /// <summary>
    /// Draws the next type from <see cref="PieceQueue"/> and places it per DR-06 (spawn
    /// placement), DR-07 (always Orientation.Spawn) and DR-08 (one free initial descent).
    /// DR-25: only legal when there is no active piece.
    /// <para>
    /// Deliberately NOT implemented here (owned by stories 003/004): gravity ticking, lock delay,
    /// DR-21 block-out / DR-22 lock-out detection and the GameOver transition. If the spawn cells
    /// are already occupied, this method simply does not place a piece and leaves
    /// <see cref="ActivePiece"/> null — story 004 (FR-021/SC-023) must detect that case and move
    /// the session to GameStatus.GameOver. Story 003/004 should also call this method again
    /// immediately after each lock resolves (DR-20), since nothing else currently calls it beyond
    /// the very first spawn in <see cref="Start"/>.
    /// </para>
    /// </summary>
    public void Spawn()
    {
        if (ActivePiece is not null)
        {
            return;
        }

        var type = PieceQueue.Draw();
        SpawnPieceOfType(type);
    }

    /// <summary>
    /// docs/specs/005-hold-piece.md DR-67: the one spawn behaviour in the system, shared by a
    /// post-lock <see cref="Spawn"/> and a hold-swapped-in piece via <see cref="Hold"/>. Callers
    /// are responsible for drawing/selecting <paramref name="type"/> and for ensuring
    /// <see cref="ActivePiece"/> is already null (no outgoing piece's cells are ever written
    /// here — that is DR-09/DR-64's job, done by the caller before this runs).
    /// </summary>
    private void SpawnPieceOfType(TetrominoType type)
    {
        var definition = TetrominoDefinition.For(type);

        var minColumnOffset = definition.SpawnOffsets.Min(o => o.ColumnOffset);
        var maxRowOffset = definition.SpawnOffsets.Max(o => o.RowOffset);

        // DR-06: leftmost occupied column at floor((10 - spawnWidth) / 2); lowest occupied cell
        // in row 19 (bottom-most buffer row).
        var spawnColumn = (Playfield.ColumnCount - definition.SpawnWidth) / 2 - minColumnOffset;
        var spawnRow = 19 - maxRowOffset;
        var position = new CellPosition(spawnColumn, spawnRow);

        var piece = new ActivePiece
        {
            Type = type,
            Orientation = Orientation.Spawn, // DR-07
            Position = position
        };

        if (!Playfield.IsValidPlacement(piece.OccupiedCells))
        {
            // DR-21/FR-021/SC-023 block out: spawn cells already occupied. The piece is not
            // placed (ActivePiece stays null) and the session ends. docs/specs/008-game-over.md
            // DR-108: a piece that fails to spawn never spawned, so PiecesSpawned is NOT
            // incremented here (unlike every other path through this method).
            CompleteGameOver(TopOutReason.BlockOut);
            return;
        }

        // DR-08: attempt exactly one free initial descent before any gravity interval elapses.
        var steppedPosition = new CellPosition(position.Column, position.Row + 1);
        var steppedCells = definition.SpawnOffsets
            .Select(o => new CellPosition(steppedPosition.Column + o.ColumnOffset, steppedPosition.Row + o.RowOffset));

        if (Playfield.IsValidPlacement(steppedCells))
        {
            piece.Position = steppedPosition;
        }

        ActivePiece = piece;
        PiecesSpawned++;
    }

    /// <summary>
    /// docs/specs/005-hold-piece.md FR-001..FR-015: stash the falling piece, or swap it with the
    /// held one. DR-70: a no-op (returns false, no state change at all) unless
    /// <see cref="HoldAvailable"/> is true. DR-72: callers must resolve a pending hold before any
    /// gravity/lock-timer advance within the same update step (<see cref="Tick"/> itself never
    /// calls this; <c>Pages/Index.cshtml.cs</c> is responsible for calling it first).
    /// </summary>
    public bool Hold()
    {
        if (!HoldAvailable) // DR-68/DR-70
        {
            return false;
        }

        var outgoingType = ActivePiece!.Type;
        HoldUsedThisPiece = true; // DR-68: at most one accepted hold per piece.
        ActivePiece = null; // FR-009/DR-70: discarded without writing any cell to the Playfield.

        TetrominoType incomingType;
        if (HeldType is null)
        {
            // DR-65: empty slot -- stash the outgoing type and draw the incoming one from the
            // queue (which refills itself per DR-13), advancing story 004's preview by one.
            HeldType = outgoingType;
            incomingType = PieceQueue.Draw();
        }
        else
        {
            // DR-66: occupied slot -- exchange types; nothing is drawn from the queue, so the
            // preview (story 004) is left completely unchanged.
            incomingType = HeldType.Value;
            HeldType = outgoingType;
        }

        // DR-67: the incoming piece is a genuinely new ActivePiece via the single spawn path --
        // standard placement, fresh timers, full reset budget, the DR-08 initial drop. This can
        // itself detect DR-21 block out and move the session to GameOver (DR-71).
        SpawnPieceOfType(incomingType);
        return true;
    }

    /// <summary>DR-28: input affects the game only while Playing with an ActivePiece.</summary>
    private bool CanAcceptInput => Status == GameStatus.Playing;

    /// <summary>FR-002/DR-29: `←` pressed — becomes/stays the active direction (DR-32), performs
    /// an immediate move (DR-31), and restarts DAS.</summary>
    public void PressLeft() => PressDirection(HorizontalDirection.Left);

    public void PressRight() => PressDirection(HorizontalDirection.Right);

    private void PressDirection(HorizontalDirection direction)
    {
        if (!CanAcceptInput)
        {
            return;
        }

        Input.PressDirection(direction);
        TryMoveActiveDirection();
    }

    /// <summary>FR-009/DR-31/DR-32: releasing a direction key clears its repeat state; if the
    /// other direction is still held it becomes active again with an immediate move (SC-011).</summary>
    public void ReleaseLeft() => ReleaseDirection(HorizontalDirection.Left);

    public void ReleaseRight() => ReleaseDirection(HorizontalDirection.Right);

    private void ReleaseDirection(HorizontalDirection direction)
    {
        if (!CanAcceptInput)
        {
            return;
        }

        var activeBefore = Input.ActiveDirection;
        Input.ReleaseDirection(direction);

        if (Input.ActiveDirection is not null && Input.ActiveDirection != activeBefore)
        {
            TryMoveActiveDirection();
        }
    }

    private void TryMoveActiveDirection()
    {
        if (ActivePiece is null || Input.ActiveDirection is not { } direction)
        {
            return;
        }

        var piece = ActivePiece;
        ApplyGroundedActionTrigger(piece, () => piece.TryMoveHorizontal(direction, Playfield));
    }

    /// <summary>FR-010/DR-33/DR-34: one quarter turn per press, no auto-repeat.</summary>
    public void RotateClockwise() => Rotate(RotationDirection.Clockwise);

    public void RotateCounterClockwise() => Rotate(RotationDirection.CounterClockwise);

    private void Rotate(RotationDirection direction)
    {
        if (!CanAcceptInput || ActivePiece is not { } piece)
        {
            return;
        }

        ApplyGroundedActionTrigger(piece, () => piece.TryRotate(direction, Playfield));
    }

    /// <summary>FR-019/DR-39: `↓` held/released. Soft drop is pure acceleration of gravity and is
    /// applied in <see cref="Tick"/>; it is never itself a lock-delay reset trigger (DR-37).</summary>
    public void PressSoftDrop()
    {
        if (CanAcceptInput)
        {
            Input.SoftDropHeld = true;
        }
    }

    public void ReleaseSoftDrop()
    {
        if (CanAcceptInput)
        {
            Input.SoftDropHeld = false;
        }
    }

    /// <summary>
    /// DR-37/DR-38/DR-18: wraps a move or rotation attempt. A rejected attempt changes nothing
    /// extra (the attempt itself already left the piece untouched, DR-38). A successful attempt
    /// on a piece that was grounded (a lock delay was running) either resets that delay — consuming
    /// one of the 15-reset budget, unless exhausted — or, if the action left the piece no longer
    /// grounded, cancels the delay without consuming a reset (DR-18) so gravity resumes. A new
    /// lowest row reached by the action replenishes the budget first (DR-17), taking precedence.
    /// A successful action on a piece that was not grounded is not a reset trigger at all.
    /// </summary>
    private bool ApplyGroundedActionTrigger(ActivePiece piece, Func<bool> action)
    {
        var wasGrounded = piece.LockTimerElapsed is not null;

        if (!action())
        {
            return false; // DR-38: rejected, inert.
        }

        UpdateLowestRowReached(piece); // DR-17/DR-29: replenish-on-new-low takes precedence.

        if (!wasGrounded)
        {
            return true; // Not a reset trigger: gravity still owns this piece (DR-37).
        }

        if (piece.IsGrounded(Playfield))
        {
            piece.TryResetLockTimer(); // DR-37: reset if budget allows; otherwise keep running.
        }
        else
        {
            piece.LockTimerElapsed = null; // DR-18: cancel, no refund; gravity resumes next Tick.
        }

        return true;
    }

    /// <summary>
    /// US-003 (FR-013/FR-014/FR-023, DR-14, DR-15, DR-26). Advances the active piece's gravity
    /// timer by <paramref name="elapsed"/>, capped per call to <see cref="MaxElapsedPerTick"/>
    /// (DR-26/EC-2), and steps the piece down one cell per whole <see cref="GravityInterval"/>
    /// that has accumulated — but only while the piece is not grounded (DR-15), and only when
    /// the step itself is a valid placement (DR-03/DR-04).
    /// <para>
    /// A zero or negative <paramref name="elapsed"/> (EC-10, e.g. a clock hiccup) is a no-op:
    /// the piece never moves up and the timer never runs backwards.
    /// </para>
    /// <para>
    /// Deliberately NOT implemented here (owned by story 004/US-004): once the piece is
    /// grounded, this method simply stops acting on it — no lock delay timer is started, no
    /// locking happens, and no next piece spawns. <see cref="ActivePiece.IsGrounded"/> is the
    /// extension point US-004 builds its lock delay on top of; <see cref="ActivePiece.LockTimerElapsed"/>,
    /// <see cref="ActivePiece.LockResetsUsed"/> and <see cref="ActivePiece.LowestRowReached"/>
    /// remain untouched placeholders.
    /// </para>
    /// </summary>
    public void Tick(TimeSpan elapsed)
    {
        if (Status != GameStatus.Playing) // DR-27: terminal status, nothing more happens.
        {
            return;
        }

        if (ActivePiece is null)
        {
            return;
        }

        if (elapsed <= TimeSpan.Zero)
        {
            // EC-10: zero/negative delta is treated as zero; never runs backwards.
            return;
        }

        var clamped = elapsed > MaxElapsedPerTick ? MaxElapsedPerTick : elapsed; // DR-26/EC-2
        _elapsedPlayTime += clamped; // docs/specs/008-game-over.md: simulated-time basis for EndedAt.
        var piece = ActivePiece;

        UpdateLowestRowReached(piece); // DR-17: baseline before this tick's movement.

        // DR-43: resolve all pending input for this step — here, DAS/ARR auto-repeat — before
        // gravity or soft drop are evaluated. The immediate move on first press already happened
        // in PressDirection; this only emits the *repeat* moves due since the last Tick.
        var repeats = Input.AdvanceAndCountRepeats(clamped);
        for (var i = 0; i < repeats && ActivePiece is not null; i++)
        {
            TryMoveActiveDirection();
        }

        if (ActivePiece is null)
        {
            return; // A move cannot lock a piece, but stay defensive for future callers.
        }

        if (piece.IsGrounded(Playfield))
        {
            // FR-016/DR-16: gravity stops acting on a grounded piece; a 0.500s lock delay
            // accumulates instead.
            piece.LockTimerElapsed = (piece.LockTimerElapsed ?? TimeSpan.Zero) + clamped;

            if (piece.LockTimerElapsed >= LockDelay)
            {
                LockActivePiece(piece); // DR-19, then DR-20/DR-22.
            }

            return;
        }

        // DR-18: the piece is not (or no longer) grounded. Cancel any running lock delay —
        // without refunding the reset budget — and resume gravity.
        piece.LockTimerElapsed = null;

        piece.FallTimerElapsed += clamped;

        // DR-39: soft drop is pure acceleration of gravity — the fall timer counts down toward
        // softDropInterval (gravityInterval / 20) instead of gravityInterval while `↓` is held.
        // FR-020: regardless of which interval applies or how much time is pending, at most one
        // cell is stepped per Tick call — excess elapsed time simply stays banked in
        // FallTimerElapsed for the next call (DR-26), so no intervening cell is ever skipped.
        var interval = Input.SoftDropHeld ? SoftDropInterval : GravityInterval;

        if (piece.FallTimerElapsed >= interval && !piece.IsGrounded(Playfield))
        {
            if (piece.TryStepDown(Playfield))
            {
                piece.FallTimerElapsed -= interval;

                if (Input.SoftDropHeld)
                {
                    piece.SoftDropCells++; // DR-41: only cells descended under soft drop count.

                    // Spec 006 FR-012/DR-81: 1 point per cell, awarded as each cell is descended,
                    // never multiplied by level.
                    AwardScore(1, ScoreSource.SoftDrop, cellCount: 1);
                }

                UpdateLowestRowReached(piece); // DR-17: replenish budget on a new lowest row.
            }

            // DR-03/DR-04: if TryStepDown somehow failed despite IsGrounded being stale/false,
            // simply stop for this tick rather than force an invalid placement.
        }
    }

    /// <summary>
    /// Spec 003 (US-001/US-002/US-004): published by the most recently resolved <see cref="HardDrop"/>
    /// call, for story 006's scoring (DR-48). Null until the first hard drop of the session; a
    /// rejected command (DR-50) leaves it unchanged rather than publishing a new outcome.
    /// </summary>
    public HardDropOutcome? LastHardDropOutcome { get; private set; }

    /// <summary>
    /// FR-001..FR-013 (spec 003): slams the active piece straight down to its landing position
    /// (DR-44) and locks it there in the same step (DR-45/DR-46) -- no gravity step, no further
    /// input and no rendered frame in between. DR-50: a no-op, returning null, unless
    /// <see cref="Status"/> is <see cref="GameStatus.Playing"/> and there is an <see cref="ActivePiece"/>;
    /// the command is discarded rather than queued for a later piece. The running lock delay (if
    /// any) is discarded, not waited out, and the 15-reset budget is neither consulted, consumed
    /// nor replenished (DR-45) -- the piece is destroyed by the lock immediately afterwards anyway.
    /// </summary>
    public HardDropOutcome? HardDrop()
    {
        if (Status != GameStatus.Playing || ActivePiece is null) // DR-50
        {
            return null;
        }

        var piece = ActivePiece;
        var distance = piece.ComputeHardDropDistance(Playfield); // DR-44

        if (distance > 0)
        {
            piece.Position = new CellPosition(piece.Position.Column, piece.Position.Row + distance);
        }

        // DR-45: the lock delay (if any was running) is discarded, not waited out; the reset
        // budget is left untouched -- it ceases to exist along with the piece in LockActivePiece.
        piece.LockTimerElapsed = null;

        var outcome = new HardDropOutcome(distance, piece.Position, piece.Type);
        LastHardDropOutcome = outcome;

        // Spec 006 FR-013/DR-81/DR-85 step 1: 2 points per cell travelled, awarded before the
        // piece's cells are written -- independent of whether the resulting lock clears anything.
        if (distance > 0)
        {
            AwardScore(distance * 2L, ScoreSource.HardDrop, cellCount: distance);
        }

        LockActivePiece(piece); // DR-19, then line clear resolution, then DR-20/DR-22.

        return outcome;
    }

    /// <summary>
    /// DR-17: whenever the piece occupies a row lower (larger index) than any it has previously
    /// occupied, its lock reset budget is replenished to <see cref="ActivePiece.MaxLockResets"/>.
    /// </summary>
    private static void UpdateLowestRowReached(ActivePiece piece)
    {
        var maxRow = piece.OccupiedCells.Max(c => c.Row);
        if (maxRow > piece.LowestRowReached)
        {
            piece.LowestRowReached = maxRow;
            piece.LockResetsUsed = 0;
        }
    }

    /// <summary>
    /// Spec 006 §5.5 DR-85 — Lock Resolution Sequence: locks <paramref name="piece"/> by writing
    /// its four occupied cells into the playfield (DR-19), evaluates lock out (DR-22) against
    /// those pre-collapse positions, then resolves any line clear (detect/collapse/score, DR-75..
    /// DR-82) before finally moving to GameOver or spawning the next piece. Steps 1-5 of DR-85 are
    /// atomic (DR-86) -- nothing else is interleaved by this method.
    /// </summary>
    private void LockActivePiece(ActivePiece piece)
    {
        var cells = piece.OccupiedCells.ToArray();

        foreach (var cell in cells)
        {
            Playfield.GetCell(cell.Row, cell.Column).Occupant = piece.Type;
        }

        ActivePiece = null;

        // docs/specs/005-hold-piece.md DR-69: hold availability is cleared by a lock, and at no
        // other moment -- in particular not by a spawn. This is what prevents an infinite hold
        // swap loop: a swapped-in piece inherits HoldUsedThisPiece = true until it locks.
        HoldUsedThisPiece = false;

        // DR-22/FR-022/SC-024 lock out: all four cells still in buffer rows (row <= 19),
        // evaluated against the *pre-collapse* positions just written (DR-85 step 3).
        var isLockOut = cells.All(c => c.Row <= 19);

        ResolveLineClear(); // DR-85 step 4: detect, collapse, counter, score -- before GameOver.

        if (isLockOut)
        {
            // DR-85 step 5 / DR-22: points and linesCleared from this lock are already applied
            // above (FR-021/SC-016/SC-011), so the final score includes the terminal clear.
            CompleteGameOver(TopOutReason.LockOut);
            return;
        }

        // DR-20/FR-020/SC-022/FR-008: the next piece spawns immediately against the
        // post-collapse playfield, so a clear that frees the spawn area averts a block out.
        Spawn();
    }

    /// <summary>
    /// Spec 006 DR-75/DR-76/DR-77/DR-80/DR-82: runs after every lock, regardless of whether any
    /// row is full (a <see cref="Domain.ClearType.None"/> result is still recorded, EC-1).
    /// Detects every full row across all 40 rows, collapses them (DR-77, no cascade per DR-78),
    /// increments <see cref="LinesCleared"/>, awards line clear points at the <see cref="Level"/>
    /// in effect before any change this clear causes (DR-84), and records
    /// <see cref="LastLineClear"/>.
    /// </summary>
    private void ResolveLineClear()
    {
        var clearedRows = Playfield.DetectFullRows(); // DR-75/DR-76.
        var result = LineClearResult.For(clearedRows, Level); // DR-79/DR-80/DR-84.

        if (clearedRows.Count > 0)
        {
            Playfield.CollapseRows(clearedRows); // DR-77/DR-78.
            LinesCleared += clearedRows.Count; // DR-82.
        }

        if (result.PointsAwarded > 0)
        {
            AwardScore(result.PointsAwarded, ScoreSource.LineClear); // DR-80.
        }

        // docs/specs/007-levels-and-speed.md DR-90/DR-95/FR-003/FR-015: re-evaluate the level
        // only here, inside line-clear resolution, between the lock (DR-19) above and the next
        // spawn (DR-20) below -- so a changed GravityInterval applies from the very next spawn
        // and never re-times a piece already in flight (there is none at this point, DR-25).
        RecomputeLevel();

        LastLineClear = result;
    }

    /// <summary>
    /// docs/specs/007-levels-and-speed.md DR-90: <c>level = min(floor(totalLinesCleared / 10) + 1,
    /// levelCap)</c>, a pure function of <see cref="LinesCleared"/> and nothing else. DR-91: never
    /// decreases -- a no-op if the derived value is not greater than the current <see cref="Level"/>.
    /// </summary>
    private void RecomputeLevel()
    {
        var derivedLevel = Math.Min(LinesCleared / 10 + 1, LevelCap);

        if (derivedLevel > Level)
        {
            Level = derivedLevel;
        }
    }

    /// <summary>
    /// Spec 006 DR-83: the sole mutator of <see cref="Score"/> -- adds a non-negative
    /// <see cref="ScoreAward"/>'s points. There is no operation that subtracts, so
    /// <see cref="Score"/> is structurally non-decreasing for the session's lifetime.
    /// </summary>
    private void AwardScore(long points, ScoreSource source, int? cellCount = null)
    {
        var award = new ScoreAward(points, source, cellCount);
        Score += award.Points;
    }

    /// <summary>
    /// docs/specs/008-game-over.md FR-001/FR-002/FR-014 (DR-100..DR-104): the single path by which
    /// a session reaches <see cref="GameStatus.GameOver"/>, called at the exact instant either
    /// top-out condition fires (block out in <see cref="SpawnPieceOfType"/>, lock out in
    /// <see cref="LockActivePiece"/>) and nowhere else (DR-100). Freezes the session (DR-101),
    /// stamps <see cref="EndedAt"/> and constructs the single immutable <see cref="CompletedRun"/>
    /// for the whole session (DR-103) from the figures as they stand at this instant -- after any
    /// terminal line clear has already been resolved by the caller (DR-104/SC-011), and excluding a
    /// blocked-out piece from <see cref="PiecesSpawned"/> (DR-108, enforced by the caller never
    /// incrementing it for that case).
    /// </summary>
    private void CompleteGameOver(TopOutReason reason)
    {
        Status = GameStatus.GameOver;
        Input.Reset(); // DR-28/EC-6: every bound key becomes inert; a held key must be re-pressed.

        var startedAt = StartedAt ?? DateTimeOffset.UtcNow;
        var endedAt = startedAt + _elapsedPlayTime;
        EndedAt = endedAt;

        CompletedRun = new CompletedRun(
            runId: Id,
            finalScore: Score,
            linesCleared: LinesCleared,
            finalLevel: Level,
            piecesSpawned: PiecesSpawned,
            topOutReason: reason,
            startedAt: startedAt,
            endedAt: endedAt);
    }
}
