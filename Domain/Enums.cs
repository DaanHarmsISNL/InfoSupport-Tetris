namespace infosupport_tetris.Domain;

/// <summary>
/// Lifecycle state of a <see cref="GameSession"/>.
/// See docs/specs/001-start-new-game.md §5.4.
/// </summary>
public enum GameStatus
{
    Ready,
    Playing,
    GameOver
}

/// <summary>
/// The seven one-sided tetrominoes. Closed set — no other value is valid (DR-05).
/// Shapes/spawn offsets are owned by story 002 (TetrominoDefinition); this spec only
/// needs the type identity for the domain model shape.
/// </summary>
public enum TetrominoType
{
    I,
    O,
    T,
    S,
    Z,
    J,
    L
}

/// <summary>
/// The four SRS rotation states (§5.4). Story 002 owns the transitions between them and
/// the Right/Inverted/Left offset tables; this spec only spawns pieces in <see cref="Spawn"/>.
/// </summary>
public enum Orientation
{
    Spawn,
    Right,
    Inverted,
    Left
}

/// <summary>
/// docs/specs/006-line-clears-and-scoring.md DR-79: classification of a single lock resolution
/// by how many rows it cleared. Closed set — a tetromino spans at most four rows (DR-05/DR-87),
/// so no value above <see cref="Tetris"/> is reachable.
/// </summary>
public enum ClearType
{
    None,
    Single,
    Double,
    Triple,
    Tetris
}

/// <summary>
/// Spec 006 §5.4: why points were awarded. <see cref="LineClear"/> is multiplied by
/// <see cref="GameSession.Level"/> (DR-80); <see cref="SoftDrop"/> and <see cref="HardDrop"/> are
/// not (DR-81).
/// </summary>
public enum ScoreSource
{
    LineClear,
    SoftDrop,
    HardDrop
}

/// <summary>
/// docs/specs/008-game-over.md §5.4 (DR-100): the closed set of ways a session can reach
/// <see cref="GameStatus.GameOver"/>. There is no third member and no "unknown" value -- a
/// <see cref="CompletedRun"/> cannot exist without one of these having fired.
/// </summary>
public enum TopOutReason
{
    /// <summary>Block Out (DR-21): a piece could not be placed at its spawn position.</summary>
    BlockOut,

    /// <summary>Lock Out (DR-22): a piece locked with all four of its cells in buffer rows.</summary>
    LockOut
}
