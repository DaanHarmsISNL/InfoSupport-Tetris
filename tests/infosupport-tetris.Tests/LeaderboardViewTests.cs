using infosupport_tetris.Domain;
using infosupport_tetris.Services;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for docs/specs/010-view-leaderboard.md, covering ranking order (DR-169), sequential
/// ranking (DR-170), the default and growing window (DR-173, DR-174) and windowing by rank and by
/// entryId (FR-012, DR-175).
/// </summary>
public class LeaderboardViewTests
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

    // SC-001/FR-001/FR-002/FR-004/FR-005: the top ten are shown, highest first, ranked 1..10.
    [Fact]
    public void GetView_OrdersByScoreDescending_AndNumbersSequentially()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        Seed(store,
            MakeEntry("Low", 100, 5, 1, now),
            MakeEntry("High", 900, 20, 3, now),
            MakeEntry("Mid", 500, 10, 2, now));

        var view = service.GetView(WindowRequest.Top(10), now);

        Assert.Equal(new[] { "High", "Mid", "Low" }, view.Rows.Select(r => r.PlayerName));
        Assert.Equal(new[] { 1, 2, 3 }, view.Rows.Select(r => r.Rank));
        Assert.Equal(3, view.EntryCount);
    }

    // SC-002/FR-001: a tie on score is broken by submittedAt ascending -- whoever got there first ranks higher.
    [Fact]
    public void GetView_BreaksScoreTieBySubmittedAtAscending()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        Seed(store,
            MakeEntry("Ans", 12_400, 31, 4, now.AddHours(2)),
            MakeEntry("Jan", 12_400, 31, 4, now));

        var view = service.GetView(WindowRequest.Top(10), now);

        Assert.Equal("Jan", view.Rows[0].PlayerName);
        Assert.Equal(1, view.Rows[0].Rank);
        Assert.Equal("Ans", view.Rows[1].PlayerName);
        Assert.Equal(2, view.Rows[1].Rank);
    }

    // EC-5/DR-169: an identical score AND submittedAt falls back to entryId ascending, deterministically.
    [Fact]
    public void GetView_BreaksRemainingTieByEntryIdAscending()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Seed(store,
            MakeEntry("B", 100, 1, 1, now, higherId),
            MakeEntry("A", 100, 1, 1, now, lowerId));

        var view = service.GetView(WindowRequest.Top(10), now);

        Assert.Equal(lowerId, view.Rows[0].EntryId);
        Assert.Equal(higherId, view.Rows[1].EntryId);
    }

    // SC-003/FR-002: ranks are sequential with no gaps, even when several scores tie.
    [Fact]
    public void GetView_RanksSequentially_WithNoGapsAcrossTiedScores()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        Seed(store,
            MakeEntry("A", 9_000, 1, 1, now),
            MakeEntry("B", 9_000, 1, 1, now.AddSeconds(1)),
            MakeEntry("C", 9_000, 1, 1, now.AddSeconds(2)),
            MakeEntry("D", 4_000, 1, 1, now));

        var view = service.GetView(WindowRequest.Top(10), now);

        Assert.Equal(new[] { 1, 2, 3, 4 }, view.Rows.Select(r => r.Rank));
    }

    // DR-173: the default window shows ranks 1-10 only, even when the board holds more entries.
    [Fact]
    public void GetView_DefaultWindow_ShowsOnlyTopTen()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 23; i++)
        {
            Seed(store, MakeEntry($"P{i}", 1000 - i, 1, 1, now.AddSeconds(i)));
        }

        var view = service.GetView(WindowRequest.Top(10), now);

        Assert.Equal(10, view.Rows.Count);
        Assert.Equal(1, view.WindowStart);
        Assert.Equal(23, view.EntryCount);
        Assert.True(view.HasMore);
    }

    // SC-008/FR-011: once the window covers the whole board, there is nothing more to show.
    [Fact]
    public void GetView_WhenWindowCoversWholeBoard_HasMoreIsFalse()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 34; i++)
        {
            Seed(store, MakeEntry($"P{i}", 1000 - i, 1, 1, now.AddSeconds(i)));
        }

        var view = service.GetView(WindowRequest.Top(34), now);

        Assert.Equal(34, view.Rows.Count);
        Assert.False(view.HasMore);
    }

    // SC-009/FR-012: a window can be requested starting at an arbitrary rank.
    [Fact]
    public void GetView_FromRank_StartsAtRequestedRank()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 50; i++)
        {
            Seed(store, MakeEntry($"P{i}", 1000 - i, 1, 1, now.AddSeconds(i)));
        }

        var view = service.GetView(WindowRequest.FromRank(startRank: 11, size: 10), now);

        Assert.Equal(11, view.WindowStart);
        Assert.Equal(11, view.Rows[0].Rank);
        Assert.Equal("P10", view.Rows[0].PlayerName);
    }

    // EC-9: a FromRank window starting past the end of the board is not an error.
    [Fact]
    public void GetView_FromRank_PastEndOfBoard_ReturnsEmptyRowsWithTrueEntryCount()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        Seed(store, MakeEntry("Only", 100, 1, 1, now));

        var view = service.GetView(WindowRequest.FromRank(startRank: 50, size: 10), now);

        Assert.Empty(view.Rows);
        Assert.Equal(1, view.EntryCount);
    }

    // SC-009/FR-012/DR-175: a window can be requested as the one containing a given entryId.
    [Fact]
    public void GetView_Containing_ReturnsWindowAroundTheEntrysRank()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        Guid? targetId = null;
        for (var i = 0; i < 412; i++)
        {
            var id = Guid.NewGuid();
            if (i == 236) // 0-indexed 236th-highest score -> rank 237
            {
                targetId = id;
            }

            Seed(store, MakeEntry($"P{i}", 1000 - i, 1, 1, now.AddSeconds(i), id));
        }

        var view = service.GetView(WindowRequest.Containing(targetId!.Value, size: 10), now);

        Assert.True(view.RequestedEntryFound);
        Assert.Equal(237, view.WindowStart);
        Assert.Contains(view.Rows, r => r.EntryId == targetId.Value && r.Rank == 237);
    }

    // EC-10: a Containing window for an entryId not on the board falls back to Top, flagged as not found.
    [Fact]
    public void GetView_Containing_UnknownEntryId_FallsBackToTopWindow()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        Seed(store, MakeEntry("Only", 100, 1, 1, now));

        var view = service.GetView(WindowRequest.Containing(Guid.NewGuid(), size: 10), now);

        Assert.False(view.RequestedEntryFound);
        Assert.Equal(1, view.WindowStart);
    }

    // SC-004/DR-171: rank is derived at read time and shifts when the board changes; the entry itself does not.
    [Fact]
    public void GetView_RankShiftsWhenHigherScoresAreAdded_EntryItselfUnaffected()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        var targetId = Guid.NewGuid();
        Seed(store, MakeEntry("Target", 500, 1, 1, now, targetId));
        Seed(store, MakeEntry("A", 100, 1, 1, now));
        Seed(store, MakeEntry("B", 200, 1, 1, now));

        var firstView = service.GetView(WindowRequest.Top(10), now);
        Assert.Equal(1, firstView.Rows.Single(r => r.EntryId == targetId).Rank);

        Seed(store, MakeEntry("C", 900, 1, 1, now));
        Seed(store, MakeEntry("D", 800, 1, 1, now));

        var secondView = service.GetView(WindowRequest.Top(10), now);
        Assert.Equal(3, secondView.Rows.Single(r => r.EntryId == targetId).Rank);
    }

    // SC-006/FR-007/DR-180: two entries with the same name are two separate rows, never merged.
    [Fact]
    public void GetView_SameNameTwice_IsTwoSeparateRows()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        Seed(store,
            MakeEntry("Daan", 15_000, 10, 2, now),
            MakeEntry("Daan", 4_000, 5, 1, now.AddSeconds(1)));

        var view = service.GetView(WindowRequest.Top(10), now);

        Assert.Equal(2, view.Rows.Count);
        Assert.Equal(new[] { 1, 2 }, view.Rows.Select(r => r.Rank));
    }

    // FR-006/DR-178: figures are shown exactly as stored -- no recomputation.
    [Fact]
    public void GetView_FiguresAreCopiedUnchanged()
    {
        var (service, store) = CreateService();
        var now = DateTimeOffset.UtcNow;
        Seed(store, MakeEntry("Big", 184_000, 140, 15, now));

        var view = service.GetView(WindowRequest.Top(10), now);

        var row = view.Rows.Single();
        Assert.Equal(184_000, row.Score);
        Assert.Equal(140, row.LinesCleared);
        Assert.Equal(15, row.Level);
    }

    // DR-186/EC-1: an empty board reports entryCount 0 and no rows -- not an error.
    [Fact]
    public void GetView_EmptyBoard_ReportsZeroEntryCount()
    {
        var (service, _) = CreateService();

        var view = service.GetView(WindowRequest.Top(10), DateTimeOffset.UtcNow);

        Assert.Empty(view.Rows);
        Assert.Equal(0, view.EntryCount);
        Assert.False(view.HasMore);
    }
}
