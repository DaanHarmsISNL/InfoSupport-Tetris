using System.Linq;
using infosupport_tetris.Domain;

namespace infosupport_tetris.Services;

/// <summary>
/// docs/specs/009-submit-score.md FR-023/FR-024/DR-121/DR-122: the one shared, durable,
/// never-reset leaderboard. DR-115 requires at most one <see cref="LeaderboardEntry"/> per
/// <see cref="CompletedRun.RunId"/>, enforced structurally rather than by a caller-side guard, so
/// two concurrent submissions of the same run resolve to one entry (SC-015, NFR-004).
/// </summary>
/// <remarks>
/// Open question 1 / DR-121 defers *where* entries live to an ADR in arc42 §09 -- this
/// implementation is an in-memory store and does not itself satisfy "survives a redeploy"
/// (FR-023). It exists so the submission contract (§5, FR-021, DR-115) can be built and tested
/// now; swapping the backing store for a durable one is the ADR's job, not a change to this
/// interface.
/// </remarks>
public interface ILeaderboardStore
{
    /// <summary>
    /// DR-115: atomically creates the entry for <paramref name="runId"/> if none exists yet, or
    /// returns the one that already does. Never throws on a duplicate <paramref name="runId"/> --
    /// that is exactly the case this method exists to resolve without a conflict error.
    /// </summary>
    LeaderboardEntry GetOrCreate(Guid runId, Func<LeaderboardEntry> createEntry);

    /// <summary>
    /// docs/specs/010-view-leaderboard.md DR-195: a read-only snapshot of every entry the board
    /// holds. 010 orders and ranks the result itself (DR-169) -- this returns the entries
    /// unordered and unfiltered, and creates, changes or deletes nothing.
    /// </summary>
    IReadOnlyCollection<LeaderboardEntry> GetAll();
}

public sealed class LeaderboardStore : ILeaderboardStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, LeaderboardEntry> _entriesByRunId = new();

    public LeaderboardEntry GetOrCreate(Guid runId, Func<LeaderboardEntry> createEntry) =>
        _entriesByRunId.GetOrAdd(runId, _ => createEntry());

    public IReadOnlyCollection<LeaderboardEntry> GetAll() => _entriesByRunId.Values.ToArray();
}
