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

/// <summary>
/// docs/specs/009-submit-score.md §5.4: where one <see cref="ScoreSubmission"/> has got to.
/// Closed set. <see cref="Submitted"/> and <see cref="Declined"/> are terminal (DR-115, DR-117).
/// </summary>
public enum SubmissionStatus
{
    /// <summary>A finished run is available and nothing has been sent. The initial state.</summary>
    Unsubmitted,

    /// <summary>An attempt is in flight. Further submit activations are ignored (DR-125). Bounded by DR-128.</summary>
    Submitting,

    /// <summary>Terminal. An entry exists for this run -- freshly created or DR-115's idempotent return.</summary>
    Submitted,

    /// <summary>The last attempt did not produce an entry, as far as the client can tell. Retryable.</summary>
    Failed,

    /// <summary>Terminal. The player skipped, or played again, without submitting. Nothing was sent.</summary>
    Declined
}

/// <summary>
/// docs/specs/009-submit-score.md §5.4: why one submission attempt failed. Closed set. The server
/// never reports <see cref="AlreadySubmitted"/> -- that guard is client-only (§5.4 note).
/// </summary>
public enum SubmissionFailureReason
{
    /// <summary>Empty or whitespace-only after normalisation (DR-109).</summary>
    NameMissing,

    /// <summary>More than 20 grapheme clusters after normalisation (DR-111).</summary>
    NameTooLong,

    /// <summary>A control or format character survived normalisation (DR-111).</summary>
    NameHasForbiddenCharacters,

    /// <summary>The figures failed DR-123's sanity validation. Server only.</summary>
    FiguresRejected,

    /// <summary>No CompletedRun is available -- the window closed (DR-106). Client only.</summary>
    RunUnavailable,

    /// <summary>This run is already Submitted; the client refuses to send again. Client only (FR-022).</summary>
    AlreadySubmitted,

    /// <summary>The request could not be sent or no response arrived. Client only.</summary>
    NetworkUnavailable,

    /// <summary>The DR-128 deadline of 10 seconds elapsed with no response. Client only.</summary>
    Timeout,

    /// <summary>The server answered, but with a failure it does not attribute to the submission.</summary>
    ServerError
}
