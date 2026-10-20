namespace infosupport_tetris.Domain;

/// <summary>
/// The single tetromino currently falling under gravity. Created on spawn, destroyed on lock.
/// See spec §5.1.
/// <para>
/// This spec (001/US-002) only implements spawn placement (DR-06/DR-07) and the one-off
/// initial descent (DR-08). <see cref="FallTimerElapsed"/>, <see cref="LockTimerElapsed"/>,
/// <see cref="LockResetsUsed"/> and <see cref="LowestRowReached"/> are placeholders for
/// gravity/lock-delay behaviour: story 003 (US-003) drives <c>FallTimerElapsed</c> and moves
/// the piece down once per <c>gravityInterval</c>; story 004 (US-004) drives
/// <c>LockTimerElapsed</c>/<c>LockResetsUsed</c>/<c>LowestRowReached</c> for groundedness,
/// lock delay and the 15-reset budget (DR-15..DR-18). None of that behaviour is implemented
/// here.
/// </para>
/// </summary>
public sealed class ActivePiece
{
    public required TetrominoType Type { get; init; }

    public Orientation Orientation { get; set; } = Orientation.Spawn;

    public CellPosition Position { get; set; }

    /// <summary>Placeholder for story 003's gravity timer. Not advanced by this spec.</summary>
    public TimeSpan FallTimerElapsed { get; set; } = TimeSpan.Zero;

    /// <summary>Placeholder for story 004's lock delay timer. Not advanced by this spec.</summary>
    public TimeSpan? LockTimerElapsed { get; set; }

    /// <summary>Placeholder for story 004's lock delay reset budget (DR-17).</summary>
    public int LockResetsUsed { get; set; }

    /// <summary>Placeholder for story 004's budget-replenishment tracking (DR-17).</summary>
    public int LowestRowReached { get; set; }

    /// <summary>
    /// Derived, never stored: the four <see cref="CellPosition"/>s obtained by applying the
    /// <see cref="TetrominoDefinition"/> offsets for <see cref="Type"/> and
    /// <see cref="Orientation"/> to <see cref="Position"/>.
    /// </summary>
    public IReadOnlyList<CellPosition> OccupiedCells
    {
        get
        {
            // Only Orientation.Spawn offsets are defined by this spec; Right/Inverted/Left
            // tables are story 002's to add. Since this spec never rotates a piece, Orientation
            // stays Spawn for the lifetime of any ActivePiece it creates.
            var offsets = TetrominoDefinition.For(Type).SpawnOffsets;
            var cells = new CellPosition[offsets.Length];
            for (var i = 0; i < offsets.Length; i++)
            {
                cells[i] = new CellPosition(
                    Position.Column + offsets[i].ColumnOffset,
                    Position.Row + offsets[i].RowOffset);
            }

            return cells;
        }
    }
}
