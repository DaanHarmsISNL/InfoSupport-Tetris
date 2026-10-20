namespace infosupport_tetris.Domain;

/// <summary>
/// The single tetromino currently falling under gravity. Created on spawn, destroyed on lock.
/// See spec §5.1.
/// <para>
/// 001/US-002 implemented spawn placement (DR-06/DR-07) and the one-off initial descent (DR-08).
/// 001/US-003 (this story) drives <see cref="FallTimerElapsed"/> via <see cref="GameSession.Tick"/>
/// and adds <see cref="IsGrounded"/> (DR-15). Story 004 (US-004) still owns
/// <see cref="LockTimerElapsed"/>/<see cref="LockResetsUsed"/>/<see cref="LowestRowReached"/> for
/// lock delay and the 15-reset budget (DR-16..DR-18) — those three remain unused placeholders here.
/// </para>
/// </summary>
public sealed class ActivePiece
{
    /// <summary>DR-17: the per-piece lock delay reset budget.</summary>
    public const int MaxLockResets = 15;

    public required TetrominoType Type { get; init; }

    public Orientation Orientation { get; set; } = Orientation.Spawn;

    public CellPosition Position { get; set; }

    /// <summary>
    /// DR-14/FR-013: time accumulated toward the next one-cell descent. Advanced by
    /// <see cref="GameSession.Tick"/>; reset (by subtracting a full <c>gravityInterval</c>)
    /// each time the piece steps down.
    /// </summary>
    public TimeSpan FallTimerElapsed { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// DR-16: time accumulated toward locking while the piece is grounded. Null while the piece
    /// is not grounded (no lock delay running); set to <see cref="TimeSpan.Zero"/> when a lock
    /// delay starts and advanced by <see cref="GameSession.Tick"/>.
    /// </summary>
    public TimeSpan? LockTimerElapsed { get; set; }

    /// <summary>DR-17: resets consumed from the budget of <see cref="MaxLockResets"/>.</summary>
    public int LockResetsUsed { get; set; }

    /// <summary>Spec 002 DR-41: cells descended while soft drop (<c>↓</c>) was held. Never scored
    /// here — story 006 reads it at lock time and resets it to 0 on every spawn.</summary>
    public int SoftDropCells { get; set; }

    /// <summary>Spec 002 §5.1: which <see cref="KickOffset"/> (1-5) the most recent successful
    /// rotation applied. Null until the first rotation. Diagnostic only in this spec.</summary>
    public int? LastKickTest { get; set; }

    /// <summary>
    /// DR-17: the lowest (largest) row index any of this piece's cells has occupied so far.
    /// Drives budget replenishment — whenever the piece reaches a new lowest row, the budget
    /// is replenished to <see cref="MaxLockResets"/> (<see cref="GameSession"/> updates this).
    /// </summary>
    public int LowestRowReached { get; set; }

    /// <summary>
    /// Story 002's extension point: requests a lock delay reset (e.g. after a successful move
    /// or rotation). Per DR-17, granted only while the piece is grounded (a lock delay is
    /// running) and the 15-reset budget is not exhausted; otherwise ignored. Returns whether a
    /// reset was granted. This spec (US-004) never calls this itself — nothing here moves or
    /// rotates the piece — but exposes it so story 002 can plug in without GameSession/ActivePiece
    /// changes.
    /// </summary>
    public bool TryResetLockTimer()
    {
        if (LockTimerElapsed is null)
        {
            return false;
        }

        if (LockResetsUsed >= MaxLockResets)
        {
            return false;
        }

        LockResetsUsed++;
        LockTimerElapsed = TimeSpan.Zero;
        return true;
    }

    /// <summary>
    /// Derived, never stored: the four <see cref="CellPosition"/>s obtained by applying the
    /// <see cref="TetrominoDefinition"/> offsets for <see cref="Type"/> and
    /// <see cref="Orientation"/> to <see cref="Position"/>.
    /// </summary>
    public IReadOnlyList<CellPosition> OccupiedCells => OccupiedCellsAt(Position, Orientation);

    /// <summary>
    /// The occupied cells this piece would have if its bounding-box top-left were
    /// <paramref name="position"/> and its orientation were <paramref name="orientation"/>,
    /// instead of its current values. Used by gravity, groundedness (DR-15), horizontal moves
    /// (DR-30) and rotation/kick testing (DR-35) to probe a candidate placement without
    /// mutating the piece.
    /// </summary>
    private static IReadOnlyList<CellPosition> OccupiedCellsAt(TetrominoType type, CellPosition position, Orientation orientation)
    {
        var offsets = TetrominoDefinition.For(type).OffsetsFor(orientation);
        var cells = new CellPosition[offsets.Length];
        for (var i = 0; i < offsets.Length; i++)
        {
            cells[i] = new CellPosition(
                position.Column + offsets[i].ColumnOffset,
                position.Row + offsets[i].RowOffset);
        }

        return cells;
    }

    private IReadOnlyList<CellPosition> OccupiedCellsAt(CellPosition position, Orientation orientation)
        => OccupiedCellsAt(Type, position, orientation);

    private IReadOnlyList<CellPosition> OccupiedCellsAt(CellPosition position) => OccupiedCellsAt(position, Orientation);

    /// <summary>
    /// DR-15 — Grounded: true exactly when moving this piece down one cell would produce an
    /// invalid placement (DR-03/DR-04) against <paramref name="playfield"/>. Recomputed on demand,
    /// never cached, so it always reflects the current piece position, orientation and stack.
    /// </summary>
    public bool IsGrounded(Playfield playfield)
    {
        var below = OccupiedCellsAt(new CellPosition(Position.Column, Position.Row + 1));
        return !playfield.IsValidPlacement(below);
    }

    /// <summary>
    /// Spec 003 — DR-44 (Hard Drop Landing): the largest <c>d</c> &gt;= 0 for which every
    /// translation of this piece straight down by 0, 1, ..., d cells is a valid placement
    /// (DR-03/DR-04). <see cref="Position"/>'s column and this piece's current
    /// <see cref="Orientation"/> are used for every candidate (so a piece rotated away from
    /// Spawn by story 002 drops correctly) and are never changed by this method — it only
    /// probes candidate positions, it does not move the piece. Every intermediate row is tested
    /// one at a time (mirroring gravity's own one-cell-at-a-time stepping), so a piece can never
    /// tunnel through the stack into a cavity beneath it (EC-4/SC-004/SC-016). For an
    /// already-grounded piece (DR-15) this returns 0.
    /// </summary>
    public int ComputeHardDropDistance(Playfield playfield)
    {
        var distance = 0;
        while (true)
        {
            var candidate = new CellPosition(Position.Column, Position.Row + distance + 1);
            if (!playfield.IsValidPlacement(OccupiedCellsAt(candidate, Orientation)))
            {
                break;
            }

            distance++;
        }

        return distance;
    }

    /// <summary>
    /// Gravity/soft-drop's one-cell descent (DR-14/DR-39), extracted so <see cref="GameSession.Tick"/>
    /// no longer has to duplicate offset math and so it respects the piece's current
    /// <see cref="Orientation"/> (story 002 may have rotated it away from Spawn).
    /// </summary>
    public bool TryStepDown(Playfield playfield)
    {
        var candidate = new CellPosition(Position.Column, Position.Row + 1);
        var candidateCells = OccupiedCellsAt(candidate, Orientation);

        if (!playfield.IsValidPlacement(candidateCells))
        {
            return false;
        }

        Position = candidate;
        return true;
    }

    /// <summary>DR-33: the orientation one quarter turn from <see cref="Orientation"/> in
    /// <paramref name="direction"/>. There is no 180° step (FR-014) — every call moves exactly
    /// one quarter turn around the <c>Spawn → Right → Inverted → Left → Spawn</c> cycle.</summary>
    public static Orientation NextOrientation(Orientation current, RotationDirection direction)
    {
        var clockwiseCycle = new[] { Orientation.Spawn, Orientation.Right, Orientation.Inverted, Orientation.Left };
        var index = Array.IndexOf(clockwiseCycle, current);
        var step = direction == RotationDirection.Clockwise ? 1 : -1;
        return clockwiseCycle[(index + step + clockwiseCycle.Length) % clockwiseCycle.Length];
    }

    /// <summary>
    /// DR-30/FR-003..FR-005: slides the piece exactly one column in <paramref name="direction"/>
    /// if, and only if, the result is a valid placement (DR-03/DR-04). A rejected move changes
    /// nothing (DR-38) — not even <see cref="Position"/> — and the caller (GameSession) decides
    /// whether this counts as a lock-delay reset trigger (DR-37).
    /// </summary>
    public bool TryMoveHorizontal(HorizontalDirection direction, Playfield playfield)
    {
        var delta = direction == HorizontalDirection.Left ? -1 : 1;
        var candidate = new CellPosition(Position.Column + delta, Position.Row);
        var candidateCells = OccupiedCellsAt(candidate, Orientation);

        if (!playfield.IsValidPlacement(candidateCells))
        {
            return false; // DR-38: inert.
        }

        Position = candidate;
        return true;
    }

    /// <summary>
    /// DR-33/DR-35/DR-36: rotates the piece one quarter turn in <paramref name="direction"/>.
    /// The O tetromino always succeeds with no displacement and no kick test (DR-36). Every
    /// other type tries the five <see cref="KickOffset"/>s of its <see cref="KickTable"/> row in
    /// order, applying the first that yields a valid placement (DR-35) and recording which test
    /// succeeded in <see cref="LastKickTest"/>. Returns false, changing nothing (DR-38), only
    /// when all five tests fail.
    /// </summary>
    public bool TryRotate(RotationDirection direction, Playfield playfield)
    {
        var targetOrientation = NextOrientation(Orientation, direction);
        var definition = TetrominoDefinition.For(Type);

        if (definition.KickTableKind == KickTableKind.None)
        {
            // DR-36: O's four orientations are identical shapes — orientation still advances,
            // but no cell moves and no kick is ever attempted.
            Orientation = targetOrientation;
            LastKickTest = null;
            return true;
        }

        var row = KickTable.RowFor(definition.KickTableKind, Orientation, targetOrientation);
        for (var i = 0; i < row.Count; i++)
        {
            var offset = row[i];
            var candidatePosition = new CellPosition(Position.Column + offset.ColumnOffset, Position.Row + offset.RowOffset);
            var candidateCells = OccupiedCellsAt(candidatePosition, targetOrientation);

            if (playfield.IsValidPlacement(candidateCells))
            {
                Position = candidatePosition;
                Orientation = targetOrientation;
                LastKickTest = i + 1; // DR-35: tests are numbered 1-5.
                return true;
            }
        }

        return false; // DR-16/DR-38: all five tests failed, nothing changes.
    }
}
