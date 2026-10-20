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
