namespace infosupport_tetris.Domain;

/// <summary>
/// The single tetromino currently falling under gravity. Created on spawn, destroyed on lock.
/// <para>
/// STUB: this spec (001/US-001) never constructs an <see cref="ActivePiece"/> — spawning,
/// gravity and locking belong to stories 002-004. The shape below exists only so that
/// <see cref="GameSession.ActivePiece"/> has a type to be nullable over; later stories should
/// extend this class (e.g. with <c>occupiedCells</c> derived from a TetrominoDefinition) rather
/// than redefine it. See spec §5.1 for the full attribute set this will eventually need:
/// Orientation, Position, fallTimerElapsed, lockTimerElapsed, lockResetsUsed, lowestRowReached.
/// </para>
/// </summary>
public sealed class ActivePiece
{
    public required TetrominoType Type { get; init; }

    public Orientation Orientation { get; set; } = Orientation.Spawn;

    public CellPosition Position { get; set; }
}
