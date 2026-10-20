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
    public IReadOnlyList<CellPosition> OccupiedCells => OccupiedCellsAt(Position);

    /// <summary>
    /// The occupied cells this piece would have if its bounding-box top-left were
    /// <paramref name="position"/> instead of <see cref="Position"/>. Used by gravity (US-003)
    /// and groundedness (DR-15) to probe a candidate placement without mutating the piece.
    /// </summary>
    private IReadOnlyList<CellPosition> OccupiedCellsAt(CellPosition position)
    {
        // Only Orientation.Spawn offsets are defined by this spec; Right/Inverted/Left
        // tables are story 002's to add. Since this spec never rotates a piece, Orientation
        // stays Spawn for the lifetime of any ActivePiece it creates.
        var offsets = TetrominoDefinition.For(Type).SpawnOffsets;
        var cells = new CellPosition[offsets.Length];
        for (var i = 0; i < offsets.Length; i++)
        {
            cells[i] = new CellPosition(
                position.Column + offsets[i].ColumnOffset,
                position.Row + offsets[i].RowOffset);
        }

        return cells;
    }

    /// <summary>
    /// DR-15 — Grounded: true exactly when moving this piece down one cell would produce an
    /// invalid placement (DR-03/DR-04) against <paramref name="playfield"/>. Recomputed on demand,
    /// never cached, so it always reflects the current piece position and stack.
    /// <para>
    /// Extension point for story 004 (US-004): once this returns true, gravity (US-003) stops
    /// acting on the piece and US-004's lock delay (<see cref="LockTimerElapsed"/>,
    /// <see cref="LockResetsUsed"/>, <see cref="LowestRowReached"/>) takes over from here.
    /// </para>
    /// </summary>
    public bool IsGrounded(Playfield playfield)
    {
        var below = OccupiedCellsAt(new CellPosition(Position.Column, Position.Row + 1));
        return !playfield.IsValidPlacement(below);
    }
}
