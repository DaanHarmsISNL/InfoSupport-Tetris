using System;
using System.Linq;
using infosupport_tetris.Domain;

namespace infosupport_tetris.Services;

/// <summary>
/// docs/specs/010-view-leaderboard.md: the read half of the Compete activity. Orders and ranks the
/// whole board (DR-169, DR-170) and answers a <see cref="WindowRequest"/> with a
/// <see cref="LeaderboardView"/> -- never the whole board (DR-176). Creates, changes and deletes
/// nothing (DR-195).
/// </summary>
public interface ILeaderboardQueryService
{
    LeaderboardView GetView(WindowRequest request, DateTimeOffset serverNow);
}

public sealed class LeaderboardQueryService : ILeaderboardQueryService
{
    private readonly ILeaderboardStore _store;

    public LeaderboardQueryService(ILeaderboardStore store) => _store = store;

    public LeaderboardView GetView(WindowRequest request, DateTimeOffset serverNow)
    {
        // DR-169: score descending, then submittedAt ascending, then entryId ascending -- a total,
        // deterministic order so two reads of an unchanged board always agree (NFR-008).
        var ordered = _store.GetAll()
            .OrderByDescending(e => e.Score)
            .ThenBy(e => e.SubmittedAt)
            .ThenBy(e => e.EntryId)
            .ToArray();

        var entryCount = ordered.Length;

        // DR-170: sequential ranking, 1..N, no gaps and no shared ranks -- rank n always means
        // "the nth row of the board".
        var ranked = new RankedEntry[ordered.Length];
        for (var i = 0; i < ordered.Length; i++)
        {
            var entry = ordered[i];
            ranked[i] = new RankedEntry(
                Rank: i + 1,
                EntryId: entry.EntryId,
                PlayerName: entry.PlayerName.Value,
                Score: entry.Score,
                LinesCleared: entry.LinesCleared,
                Level: entry.Level,
                SubmittedAt: entry.SubmittedAt);
        }

        var (windowStart, requestedEntryFound) = ResolveWindowStart(request, ranked);

        var skip = Math.Max(0, windowStart - 1);
        var rows = ranked.Skip(skip).Take(request.Size).ToArray();

        return new LeaderboardView(rows, windowStart, request.Size, entryCount, serverNow, requestedEntryFound);
    }

    private static (int WindowStart, bool RequestedEntryFound) ResolveWindowStart(WindowRequest request, RankedEntry[] ranked)
    {
        switch (request.Mode)
        {
            case WindowMode.FromRank:
                // EC-9: a startRank past the end is not an error -- an empty Rows and the true
                // entryCount are returned by the caller.
                return (Math.Max(1, request.StartRank ?? 1), true);

            case WindowMode.Containing:
                // EC-10: an entryId not on the board is not an error -- fall back to the Top
                // window and flag that it was not found; what to say about that is story 011's job.
                var match = Array.Find(ranked, r => r.EntryId == request.EntryId);
                return match is null ? (1, false) : (match.Rank, true);

            case WindowMode.Top:
            default:
                return (1, true);
        }
    }
}
