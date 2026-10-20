using System;
using System.Linq;
using infosupport_tetris.Domain;

namespace infosupport_tetris.Services;

/// <summary>
/// docs/specs/011-highlight-my-run.md: resolves which row of story 010's board is "my run" and
/// where it stood. Reuses <see cref="ILeaderboardQueryService"/> for every ordering and windowing
/// decision (FR-007, DR-210) and never defines a rank of its own. Pure and stateless so every
/// scenario in §4 runs headlessly, with an injectable entryId and board reading (NFR-008).
/// </summary>
public static class RunHighlightResolver
{
    /// <summary>DR-212: two neighbours above and two below -- a deliberate, non-tunable choice (§5.3).</summary>
    private const int NeighbourCount = 2;

    /// <summary>
    /// Resolves the highlight for <paramref name="entryId"/> against <paramref name="currentView"/>
    /// (whatever window the board is currently showing -- the default top 10, or a larger one the
    /// player has already expanded to, so FR-010 falls out as a consequence rather than a special
    /// case). Returns null when the entry cannot be matched in the board at all (DR-202, DR-204) --
    /// the caller shows the board exactly as it would without a highlight.
    /// </summary>
    public static RunHighlight? Resolve(
        ILeaderboardQueryService queryService,
        LeaderboardView currentView,
        Guid entryId,
        DateTimeOffset serverNow)
    {
        // DR-199: matched solely by entryId, nothing else.
        var withinCurrentView = currentView.Rows.FirstOrDefault(r => r.EntryId == entryId);
        if (withinCurrentView is not null)
        {
            var placement = new Placement(withinCurrentView.Rank, currentView.EntryCount, serverNow, HighlightPlacementCase.WithinDefaultView);
            return new RunHighlight(entryId, placement, Context: null);
        }

        // A single-row lookup purely to locate the rank -- not to render it (DR-202: never
        // fabricate a row; this is only ever used to ask "where is it", and the answer feeds a
        // second, explicit request for the context window below).
        var located = queryService.GetView(WindowRequest.Containing(entryId, size: 1), serverNow);
        if (!located.RequestedEntryFound || located.Rows.Count == 0)
        {
            return null; // HighlightPlacementCase.Unavailable (DR-202, DR-204): EC-1, SC-019.
        }

        var rank = located.Rows[0].Rank;
        var currentViewLastRank = currentView.WindowStart - 1 + currentView.Rows.Count;

        // DR-212: the row plus its two nearest neighbours each side, clipped at the board's edges
        // by the service's own Skip/Take (DR-213.3) -- no placeholder rows are invented here.
        var contextStart = Math.Max(1, rank - NeighbourCount);
        var contextWindow = queryService.GetView(WindowRequest.FromRank(contextStart, size: NeighbourCount * 2 + 1), serverNow);

        // DR-213.1/FR-010: never repeat a row the current view already shows.
        var contextRows = contextWindow.Rows.Where(r => r.Rank > currentViewLastRank).ToArray();

        // DR-213.2: no break when the slice picks up exactly where the current view left off.
        var showsBreak = contextRows[0].Rank != currentViewLastRank + 1;

        var outsidePlacement = new Placement(rank, currentView.EntryCount, serverNow, HighlightPlacementCase.OutsideDefaultView);
        return new RunHighlight(entryId, outsidePlacement, new PlacementContext(contextRows, showsBreak));
    }
}
