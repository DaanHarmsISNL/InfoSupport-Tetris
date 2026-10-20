namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/010-view-leaderboard.md §5.3: one row of the board -- one stored
/// <see cref="LeaderboardEntry"/> plus the rank it holds in the ordering at the moment the view
/// was produced (DR-171). Immutable; recomputed per read, never stored.
/// </summary>
public sealed record RankedEntry(
    int Rank,
    Guid EntryId,
    string PlayerName,
    long Score,
    int LinesCleared,
    int Level,
    DateTimeOffset SubmittedAt);

/// <summary>
/// docs/specs/010-view-leaderboard.md §5.1: one answer to one read of the board -- a contiguous
/// window of ranked rows, plus everything the reader needs to understand what they are looking
/// at. Derived, transient and never persisted (DR-191).
/// </summary>
/// <param name="Rows">The rows to render, already in display order (DR-169).</param>
/// <param name="WindowStart">The rank of the first row in the window; 1 for the default view (DR-173).</param>
/// <param name="WindowSize">How many ranks the window asked for.</param>
/// <param name="EntryCount">How many entries the whole board holds (0 means genuinely empty, DR-186).</param>
/// <param name="GeneratedAt">The server clock reading at the moment this view was produced (DR-172).</param>
/// <param name="RequestedEntryFound">
/// DR-175/EC-10: only meaningful for <see cref="WindowMode.Containing"/> -- false when the requested
/// entryId is not on the board, in which case the view falls back to the <see cref="WindowMode.Top"/>
/// window. Story 011 decides what to say about a false value.
/// </param>
public sealed record LeaderboardView(
    IReadOnlyList<RankedEntry> Rows,
    int WindowStart,
    int WindowSize,
    int EntryCount,
    DateTimeOffset GeneratedAt,
    bool RequestedEntryFound = true)
{
    /// <summary>FR-009/FR-011: whether a show-more action should still be offered.</summary>
    public bool HasMore => WindowStart - 1 + Rows.Count < EntryCount;
}

/// <summary>docs/specs/010-view-leaderboard.md §5.4: how a <see cref="WindowRequest"/> picks its slice.</summary>
public enum WindowMode
{
    /// <summary>Ranks 1 to Size. The default view (DR-173).</summary>
    Top,

    /// <summary>Ranks StartRank to StartRank + Size - 1.</summary>
    FromRank,

    /// <summary>The window that includes the rank of EntryId (DR-175) -- story 011's entry point.</summary>
    Containing
}

/// <summary>docs/specs/010-view-leaderboard.md §5.3: which slice of the ranked board is being asked for. Immutable.</summary>
public sealed record WindowRequest(WindowMode Mode, int Size = 10, int? StartRank = null, Guid? EntryId = null)
{
    public static WindowRequest Top(int size = 10) => new(WindowMode.Top, size);

    public static WindowRequest FromRank(int startRank, int size = 10) => new(WindowMode.FromRank, size, StartRank: startRank);

    public static WindowRequest Containing(Guid entryId, int size = 10) => new(WindowMode.Containing, size, EntryId: entryId);
}
