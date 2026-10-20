using infosupport_tetris.Domain;
using infosupport_tetris.Services;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for docs/specs/011-highlight-my-run.md, covering which row is matched (DR-199, DR-200),
/// in-place highlighting inside the current view (DR-211), appended context with and without a
/// break (DR-212, DR-213), edge truncation (EC-3, EC-4, EC-5), the moving-target rank (DR-209) and
/// an entry the board does not return (DR-202, DR-204). <see cref="RunHighlightResolver"/> reuses
/// <see cref="ILeaderboardQueryService"/>'s own ordering and windowing rather than reimplementing
/// one (FR-007, DR-210).
/// </summary>
public class RunHighlightTests
{
    private static LeaderboardEntry MakeEntry(string name, long score, int linesCleared, int level, DateTimeOffset submittedAt, Guid? entryId = null) =>
        new(
            entryId: entryId ?? Guid.NewGuid(),
            playerName: PlayerName.TryCreate(name, out var playerName, out _) ? playerName! : throw new InvalidOperationException(),
            score: score,
            linesCleared: linesCleared,
            level: level,
            runId: Guid.NewGuid(),
            submittedAt: submittedAt);

    private static (ILeaderboardQueryService Service, ILeaderboardStore Store) CreateService()
    {
        var store = new LeaderboardStore();
        return (new LeaderboardQueryService(store), store);
    }

    private static void Seed(ILeaderboardStore store, params LeaderboardEntry[] entries)
    {
        foreach (var entry in entries)
        {
            store.GetOrCreate(entry.RunId, () => entry);
        }
    }

    private static Guid SeedBoard(ILeaderboardStore store, int count, DateTimeOffset now, int targetIndexFromTop)
    {
        Guid targetId = default;
        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            if (i == targetIndexFromTop)
            {
                targetId = id;
            }

            Seed(store, MakeEntry($"P{i}", count - i, 1, 1, now.AddSeconds(i), id));
        }

        return targetId;
    }

    // SC-001/FR-001/FR-003/FR-008: a top-10 run is highlighted in place, with no context appended.
    [Fact]
    public void Resolve_RowWithinCurrentView_IsHighlightedInPlace_NoContext()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var targetId = SeedBoard(store, 20, now, targetIndexFromTop: 3); // rank 4

        var defaultView = service.GetView(WindowRequest.Top(10), now);
        var highlight = RunHighlightResolver.Resolve(service, defaultView, targetId, now);

        Assert.NotNull(highlight);
        Assert.Equal(HighlightPlacementCase.WithinDefaultView, highlight!.Placement.Case);
        Assert.Equal(4, highlight.Placement.Rank);
        Assert.Equal(20, highlight.Placement.TotalEntries);
        Assert.Equal("You placed 4th of 20", highlight.Placement.Statement);
        Assert.Null(highlight.Context);
    }

    // SC-002/FR-006/FR-009: a run outside the default view is highlighted with its two neighbours
    // each side appended behind a break.
    [Fact]
    public void Resolve_RowOutsideCurrentView_AppendsTwoNeighboursEachSide_WithBreak()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var targetId = SeedBoard(store, 183, now, targetIndexFromTop: 46); // rank 47

        var defaultView = service.GetView(WindowRequest.Top(10), now);
        var highlight = RunHighlightResolver.Resolve(service, defaultView, targetId, now);

        Assert.NotNull(highlight);
        Assert.Equal(HighlightPlacementCase.OutsideDefaultView, highlight!.Placement.Case);
        Assert.Equal(47, highlight.Placement.Rank);
        Assert.Equal("You placed 47th of 183", highlight.Placement.Statement);

        var context = highlight.Context;
        Assert.NotNull(context);
        Assert.True(context!.ShowsBreak);
        Assert.Equal(new[] { 45, 46, 47, 48, 49 }, context.Rows.Select(r => r.Rank));
        Assert.Contains(context.Rows, r => r.EntryId == targetId && r.Rank == 47);
    }

    // SC-003/FR-010: once the current view already contains the row (the player expanded far
    // enough), it is highlighted in place and no context is appended -- never shown twice.
    [Fact]
    public void Resolve_WhenExpandedViewAlreadyContainsRow_IsWithinView_ContextDropped()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var targetId = SeedBoard(store, 183, now, targetIndexFromTop: 46); // rank 47

        var expandedView = service.GetView(WindowRequest.Top(50), now);
        var highlight = RunHighlightResolver.Resolve(service, expandedView, targetId, now);

        Assert.NotNull(highlight);
        Assert.Equal(HighlightPlacementCase.WithinDefaultView, highlight!.Placement.Case);
        Assert.Null(highlight.Context);
    }

    // SC-004/FR-011: ranking 11th is contiguous with the default top 10 -- no break.
    [Fact]
    public void Resolve_RowJustBelowDefaultView_ShowsNoBreak()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var targetId = SeedBoard(store, 60, now, targetIndexFromTop: 10); // rank 11

        var defaultView = service.GetView(WindowRequest.Top(10), now);
        var highlight = RunHighlightResolver.Resolve(service, defaultView, targetId, now);

        Assert.NotNull(highlight);
        Assert.False(highlight!.Context!.ShowsBreak);
        Assert.Equal(11, highlight.Context.Rows[0].Rank);
        Assert.Equal("You placed 11th of 60", highlight.Placement.Statement);
    }

    // SC-005/FR-011: last on the board has neighbours above only, no placeholder rows below.
    [Fact]
    public void Resolve_RowAtBottomOfBoard_HasNoNeighboursBelow()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var targetId = SeedBoard(store, 183, now, targetIndexFromTop: 182); // rank 183, last

        var defaultView = service.GetView(WindowRequest.Top(10), now);
        var highlight = RunHighlightResolver.Resolve(service, defaultView, targetId, now);

        Assert.NotNull(highlight);
        Assert.Equal(new[] { 181, 182, 183 }, highlight!.Context!.Rows.Select(r => r.Rank));
        Assert.Equal("You placed 183rd of 183", highlight.Placement.Statement);
    }

    // SC-006/FR-006/FR-008: topping the board needs no context at all.
    [Fact]
    public void Resolve_RowAtRankOne_IsWithinView_NoContext()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var targetId = SeedBoard(store, 183, now, targetIndexFromTop: 0); // rank 1

        var defaultView = service.GetView(WindowRequest.Top(10), now);
        var highlight = RunHighlightResolver.Resolve(service, defaultView, targetId, now);

        Assert.NotNull(highlight);
        Assert.Equal(HighlightPlacementCase.WithinDefaultView, highlight!.Placement.Case);
        Assert.Equal("You placed 1st of 183", highlight.Placement.Statement);
        Assert.Null(highlight.Context);
    }

    // SC-007/FR-002/FR-003/DR-199: a colleague with the same name and a near-identical score is
    // never marked -- only the entryId that matches is.
    [Fact]
    public void Resolve_NeverMatchesByNameOrScore_OnlyByEntryId()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var otherId = Guid.NewGuid();
        var mineId = Guid.NewGuid();
        Seed(store,
            MakeEntry("Daan", 12_450, 30, 4, now, otherId),
            MakeEntry("Daan", 12_400, 30, 4, now.AddSeconds(1), mineId));

        var defaultView = service.GetView(WindowRequest.Top(10), now);
        var highlight = RunHighlightResolver.Resolve(service, defaultView, mineId, now);

        Assert.NotNull(highlight);
        Assert.Equal(mineId, highlight!.EntryId);
        Assert.Equal(2, highlight.Placement.Rank);
    }

    // SC-008/FR-001/FR-003/FR-007: a tied score marks the lower-ranked of the two rows -- the one
    // whose entryId actually matches, derived from 010's own tie-break.
    [Fact]
    public void Resolve_TiedScore_MarksTheMatchingRow_NotTheOtherTiedRow()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        Seed(store,
            MakeEntry("X", 9_000, 1, 1, now, firstId),
            MakeEntry("E", 9_000, 1, 1, now.AddSeconds(1), secondId));

        var defaultView = service.GetView(WindowRequest.Top(10), now);
        var highlight = RunHighlightResolver.Resolve(service, defaultView, secondId, now);

        Assert.NotNull(highlight);
        Assert.Equal(2, highlight!.Placement.Rank);
    }

    // SC-019/EC-1/DR-202/DR-204: an entry the board does not return is not invented -- no
    // highlight, no placement, no crash.
    [Fact]
    public void Resolve_EntryIdNotOnBoard_ReturnsNull()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        Seed(store, MakeEntry("Only", 100, 1, 1, now));

        var defaultView = service.GetView(WindowRequest.Top(10), now);
        var highlight = RunHighlightResolver.Resolve(service, defaultView, Guid.NewGuid(), now);

        Assert.Null(highlight);
    }

    // DR-209/SC-015: rank is a reading at one instant -- a later read of a changed board produces
    // a different, equally correct Placement, not an inconsistency.
    [Fact]
    public void Resolve_RankReflectsTheBoardAsReadAtEachInstant()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var targetId = Guid.NewGuid();
        Seed(store, MakeEntry("Mine", 500, 1, 1, now, targetId));

        var firstView = service.GetView(WindowRequest.Top(10), now);
        var firstHighlight = RunHighlightResolver.Resolve(service, firstView, targetId, now);
        Assert.Equal(1, firstHighlight!.Placement.Rank);
        Assert.Equal(1, firstHighlight.Placement.TotalEntries);

        Seed(store, MakeEntry("Better", 900, 1, 1, now.AddSeconds(1)));

        var secondView = service.GetView(WindowRequest.Top(10), now);
        var secondHighlight = RunHighlightResolver.Resolve(service, secondView, targetId, now);
        Assert.Equal(2, secondHighlight!.Placement.Rank);
        Assert.Equal(2, secondHighlight.Placement.TotalEntries);
    }

    // SUC-002/DR-210: 011 derives rank entirely from 010's ordering; this is a sanity check that
    // the resolver never produces a rank disagreeing with the query service's own ranked rows.
    [Fact]
    public void Resolve_RankAlwaysAgreesWithQueryServicesOwnOrdering()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var targetId = SeedBoard(store, 30, now, targetIndexFromTop: 14); // rank 15

        var defaultView = service.GetView(WindowRequest.Top(10), now);
        var highlight = RunHighlightResolver.Resolve(service, defaultView, targetId, now);

        var fullBoard = service.GetView(WindowRequest.Top(30), now);
        var expectedRank = fullBoard.Rows.Single(r => r.EntryId == targetId).Rank;

        Assert.Equal(expectedRank, highlight!.Placement.Rank);
    }
}
