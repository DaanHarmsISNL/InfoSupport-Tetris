namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/009-submit-score.md §5.1: the central entity of the Compete activity -- one durable,
/// immutable record of one submitted run. Nothing updates any attribute after construction
/// (DR-118): there is no setter, no edit path and no correction path anywhere on this type.
/// </summary>
public sealed class LeaderboardEntry
{
    /// <summary>DR-119: server-generated, globally unique, stable, returned to the client on every success.</summary>
    public Guid EntryId { get; }

    /// <summary>A display label, never an identity (DR-112).</summary>
    public PlayerName PlayerName { get; }

    /// <summary><see cref="CompletedRun.FinalScore"/> as submitted (DR-120) -- never recomputed.</summary>
    public long Score { get; }

    /// <summary><see cref="CompletedRun.LinesCleared"/> as submitted (DR-120).</summary>
    public int LinesCleared { get; }

    /// <summary><see cref="CompletedRun.FinalLevel"/> as submitted (DR-120).</summary>
    public int Level { get; }

    /// <summary>DR-115: the idempotency key. Unique across all entries -- enforced by the store.</summary>
    public Guid RunId { get; }

    /// <summary>DR-124: the server's own UTC clock reading at creation; any client-supplied value is ignored.</summary>
    public DateTimeOffset SubmittedAt { get; }

    public LeaderboardEntry(
        Guid entryId,
        PlayerName playerName,
        long score,
        int linesCleared,
        int level,
        Guid runId,
        DateTimeOffset submittedAt)
    {
        EntryId = entryId;
        PlayerName = playerName;
        Score = score;
        LinesCleared = linesCleared;
        Level = level;
        RunId = runId;
        SubmittedAt = submittedAt;
    }
}
