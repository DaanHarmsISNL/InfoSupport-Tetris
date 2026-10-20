namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/008-game-over.md §5.1: the immutable record of one finished playthrough -- the
/// hand-off seam to story 009 (name entry and score submission). Created exactly once, at the
/// moment a <see cref="GameSession"/> transitions to <see cref="GameStatus.GameOver"/> (DR-103),
/// from copies of the session's frozen figures -- never a view onto the session itself, so it
/// cannot drift and cannot be used to reach back into gameplay state (§5.2). It carries no player
/// name and no identity of any kind (NFR-008): story 009 is the first thing to associate a name
/// with a <see cref="RunId"/>.
/// </summary>
public sealed class CompletedRun
{
    /// <summary>DR-107: equals the finished <see cref="GameSession.Id"/>, unique among all runs played on the page.</summary>
    public Guid RunId { get; }

    /// <summary>DR-104: <see cref="GameSession.Score"/> as frozen. Owned by story 006 -- read here, never computed.</summary>
    public long FinalScore { get; }

    /// <summary>DR-104: <see cref="GameSession.LinesCleared"/> as frozen. Owned by story 006.</summary>
    public int LinesCleared { get; }

    /// <summary>DR-104: <see cref="GameSession.Level"/> as frozen. Owned by story 007.</summary>
    public int FinalLevel { get; }

    /// <summary>
    /// DR-104/DR-108: <see cref="GameSession.PiecesSpawned"/> as frozen, excluding a piece that
    /// failed to spawn by block out -- the count is of pieces that actually entered play.
    /// </summary>
    public int PiecesSpawned { get; }

    /// <summary>Which of the two Guideline top-out conditions ended the run (DR-100).</summary>
    public TopOutReason TopOutReason { get; }

    /// <summary>Copied from <see cref="GameSession.StartedAt"/> at the moment of creation.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>When the session moved to <see cref="GameStatus.GameOver"/>; always &gt;= <see cref="StartedAt"/>.</summary>
    public DateTimeOffset EndedAt { get; }

    /// <summary>Derived: <see cref="EndedAt"/> - <see cref="StartedAt"/>. Recorded for completeness (open question 3); not required to be displayed.</summary>
    public TimeSpan Duration => EndedAt - StartedAt;

    public CompletedRun(
        Guid runId,
        long finalScore,
        int linesCleared,
        int finalLevel,
        int piecesSpawned,
        TopOutReason topOutReason,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt)
    {
        RunId = runId;
        FinalScore = finalScore;
        LinesCleared = linesCleared;
        FinalLevel = finalLevel;
        PiecesSpawned = piecesSpawned;
        TopOutReason = topOutReason;
        StartedAt = startedAt;
        EndedAt = endedAt;
    }
}
