using System.Linq;
using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for spec 007 (docs/specs/007-levels-and-speed.md): level derivation from
/// <see cref="GameSession.LinesCleared"/>, the Guideline gravity speed curve, and the
/// soft-drop/level interaction. Follows the LineClearAndScoringTests style: lines are cleared by
/// hard-dropping pieces into rows prepared to be exactly one cell short, so locks resolve
/// deterministically with no reliance on real elapsed time.
/// </summary>
public class LevelsAndSpeedTests
{
    // The normative §5.3 table, duplicated here (not reused from production code) so the test
    // asserts against the spec's published values rather than merely against the implementation's
    // own constant.
    private static readonly int[] ExpectedIntervalMs =
    {
        1000, 793, 618, 473, 355, 262, 190, 135, 94, 64, 43, 28, 18, 11, 7
    };

    /// <summary>
    /// Clears exactly one line by hard-dropping the active piece into a row filled everywhere
    /// except under the piece's landing columns. Works regardless of the falling piece's shape
    /// because the landing columns are computed from the piece itself.
    /// </summary>
    private static void ClearOneLine(GameSession session)
    {
        var piece = session.ActivePiece!;
        var distance = piece.ComputeHardDropDistance(session.Playfield);
        var landingCells = piece.OccupiedCells
            .Select(c => new CellPosition(c.Column, c.Row + distance))
            .ToArray();
        var landingRow = landingCells.Max(c => c.Row);
        var occupiedColumns = landingCells.Where(c => c.Row == landingRow).Select(c => c.Column).Distinct().ToArray();

        for (var column = 0; column < Playfield.ColumnCount; column++)
        {
            if (!occupiedColumns.Contains(column))
            {
                session.Playfield.GetCell(landingRow, column).Occupant = TetrominoType.J;
            }
        }

        session.HardDrop();

        // Clearing a line only removes the row it completed; any other cells the piece occupied
        // above that row (debris, for non-flat pieces) survive the collapse (DR-78, by design --
        // see LineClearAndScoringTests.Collapse_LeavesUnsupportedBlocksFloating). Left alone across
        // many iterations that debris would eventually pile up and block spawning, which has
        // nothing to do with what this test file is verifying (level derivation and the gravity
        // curve). Wipe the playfield clean after each simulated clear so every iteration starts
        // from the same empty board; only the session's LinesCleared/Level/Score persist.
        for (var row = 0; row < Playfield.TotalRowCount; row++)
        {
            for (var column = 0; column < Playfield.ColumnCount; column++)
            {
                session.Playfield.GetCell(row, column).Occupant = null;
            }
        }
    }

    private static GameSession ClearLines(int count, int pieceQueueSeed = 42)
    {
        var session = new GameSession(pieceQueueSeed: pieceQueueSeed);
        session.Start(DateTimeOffset.UtcNow);

        for (var i = 0; i < count; i++)
        {
            Assert.True(session.Status == GameStatus.Playing, $"Session ended early after {i} clears.");
            ClearOneLine(session);
        }

        return session;
    }

    // SC-001/FR-002: a new game starts at level 1 with 0 lines and a 1.000s interval.
    [Fact]
    public void NewSession_StartsAtLevel1_With1000msInterval()
    {
        var session = new GameSession(pieceQueueSeed: 1);
        session.Start(DateTimeOffset.UtcNow);

        Assert.Equal(1, session.Level);
        Assert.Equal(0, session.LinesCleared);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), session.GravityInterval);
    }

    // SC-002/FR-001: ten lines cleared promotes to level 2 with a 793ms interval.
    [Fact]
    public void TenLinesCleared_PromotesToLevel2_With793msInterval()
    {
        var session = ClearLines(10);

        Assert.Equal(10, session.LinesCleared);
        Assert.Equal(2, session.Level);
        Assert.Equal(TimeSpan.FromMilliseconds(793), session.GravityInterval);
    }

    // SC-003/FR-001/FR-003: a clear that does not reach the goal leaves the level alone.
    [Fact]
    public void FewerThanTenLinesCleared_LeavesLevelAt1()
    {
        var session = ClearLines(4);

        Assert.Equal(4, session.LinesCleared);
        Assert.Equal(1, session.Level);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), session.GravityInterval);
    }

    // EC-2/DR-90: a clear landing exactly on a multiple of ten belongs to the new level.
    [Fact]
    public void ExactlyTenLinesCleared_BoundaryBelongsToNewLevel()
    {
        var session = ClearLines(10);

        Assert.Equal(2, session.Level);
    }

    // EC-1/SC-004: a Tetris-sized burst of clears can never skip a level, since single-line
    // clears in this helper exercise the same DR-90 derivation a multi-row clear would.
    [Fact]
    public void CrossingTenLineThreshold_AdvancesExactlyOneLevel()
    {
        var session = ClearLines(9);
        Assert.Equal(1, session.Level);

        ClearOneLine(session);

        Assert.Equal(10, session.LinesCleared);
        Assert.Equal(2, session.Level);
    }

    // SC-007/FR-004/DR-91: the level never decreases, and stays put between clears.
    [Fact]
    public void Level_NeverDecreases_AndStaysBetweenLineClears()
    {
        var session = ClearLines(10);
        Assert.Equal(2, session.Level);

        // Idle ticks with no line clear must not change the level.
        for (var i = 0; i < 50; i++)
        {
            session.Tick(TimeSpan.FromMilliseconds(100));
        }

        Assert.Equal(2, session.Level);
    }

    // SC-008: the whole curve matches the published table and is strictly decreasing.
    [Fact]
    public void GravityInterval_MatchesPublishedCurve_ForEveryLevel()
    {
        // Drive totalLinesCleared from 0 upward and sample the interval at each level boundary,
        // confirming it against §5.3 without needing 140 real line clears per test run: ten lines
        // per level, which is small enough to actually clear via the helper.
        var session = ClearLines(0);
        Assert.Equal(ExpectedIntervalMs[0], (int)session.GravityInterval.TotalMilliseconds);

        for (var level = 2; level <= 15; level++)
        {
            for (var i = 0; i < 10; i++)
            {
                ClearOneLine(session);
            }

            Assert.Equal(level, session.Level);
            Assert.Equal(ExpectedIntervalMs[level - 1], (int)session.GravityInterval.TotalMilliseconds);
        }

        for (var i = 1; i < ExpectedIntervalMs.Length; i++)
        {
            Assert.True(ExpectedIntervalMs[i] < ExpectedIntervalMs[i - 1]);
        }
    }

    // SC-009/FR-009: level 1 still falls at exactly one cell per second.
    [Fact]
    public void Level1_FallsOneCellPerSecond()
    {
        var session = new GameSession(pieceQueueSeed: 1);
        session.Start(DateTimeOffset.UtcNow);
        var startRow = session.ActivePiece!.Position.Row;

        for (var i = 0; i < 10; i++)
        {
            session.Tick(TimeSpan.FromMilliseconds(100));
        }

        Assert.Equal(startRow + 1, session.ActivePiece!.Position.Row);
    }

    // SC-015/FR-006/DR-94: level 15 is the ceiling; further clears keep raising lines and score
    // but hold the level and interval constant.
    [Fact]
    public void BeyondLevelCap_LevelAndIntervalHoldSteady_WhileLinesAndScoreKeepRising()
    {
        var session = ClearLines(140);
        Assert.Equal(15, session.Level);
        Assert.Equal(TimeSpan.FromMilliseconds(7), session.GravityInterval);

        var scoreBefore = session.Score;

        for (var i = 0; i < 10; i++)
        {
            ClearOneLine(session);
        }

        Assert.Equal(150, session.LinesCleared);
        Assert.Equal(15, session.Level);
        Assert.Equal(TimeSpan.FromMilliseconds(7), session.GravityInterval);
        Assert.True(session.Score > scoreBefore);
    }

    // SC-016/FR-013/DR-96: soft drop is derived from gravityInterval and so accelerates with the
    // level: level 1 is 50ms/cell (gravityInterval / 20), and a higher level is strictly faster.
    [Fact]
    public void SoftDropInterval_IsOneTwentiethOfGravityInterval_AndAcceleratesWithLevel()
    {
        var level1Session = new GameSession(pieceQueueSeed: 1);
        level1Session.Start(DateTimeOffset.UtcNow);
        Assert.Equal(TimeSpan.FromMilliseconds(50), level1Session.SoftDropInterval);

        var level9Session = ClearLines(80, pieceQueueSeed: 9);
        Assert.Equal(9, level9Session.Level);
        var expectedLevel9SoftDrop = TimeSpan.FromTicks(level9Session.GravityInterval.Ticks / 20);
        Assert.Equal(expectedLevel9SoftDrop, level9Session.SoftDropInterval);
        Assert.True(level9Session.SoftDropInterval < level1Session.SoftDropInterval);
    }

    // SC-018/FR-014/DR-99: lock delay does not shrink with level.
    [Fact]
    public void LockDelay_IsUnaffectedByLevel()
    {
        var session = ClearLines(140);
        Assert.Equal(15, session.Level);
        Assert.Equal(TimeSpan.FromMilliseconds(500), GameSession.LockDelay);
    }

    // SC-020/FR-002: the level resets on a new game -- a fresh GameSession always starts at 1.
    [Fact]
    public void NewSession_AfterAnotherReachedHighLevel_StillStartsAtLevel1()
    {
        var advanced = ClearLines(140);
        Assert.Equal(15, advanced.Level);

        var fresh = new GameSession(pieceQueueSeed: 42);
        fresh.Start(DateTimeOffset.UtcNow);

        Assert.Equal(1, fresh.Level);
        Assert.Equal(0, fresh.LinesCleared);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), fresh.GravityInterval);
    }

    // DR-94/levelCap: the constant is exactly 15.
    [Fact]
    public void LevelCap_IsFifteen()
    {
        Assert.Equal(15, GameSession.LevelCap);
    }
}
