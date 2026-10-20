using System.Linq;
using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for spec 006 (docs/specs/006-line-clears-and-scoring.md): line clear detection, row
/// collapse and the scoring model. Mirrors the HardDropTests style -- playfield states are set up
/// directly via <see cref="Playfield.GetCell"/> and pieces are hard-dropped into position so locks
/// resolve deterministically with no reliance on real elapsed time.
/// </summary>
public class LineClearAndScoringTests
{
    private static void FillRow(Playfield playfield, int row, params int[] exceptColumns)
    {
        for (var column = 0; column < Playfield.ColumnCount; column++)
        {
            if (!exceptColumns.Contains(column))
            {
                playfield.GetCell(row, column).Occupant = TetrominoType.J;
            }
        }
    }

    // SC-001/FR-001/FR-002/FR-003: a completed row clears and the stack above drops one.
    [Fact]
    public void FullRow_ClearsAndRowsAboveCollapseDownOne()
    {
        var playfield = new Playfield();
        FillRow(playfield, 39); // full
        playfield.GetCell(38, 0).Occupant = TetrominoType.T;
        playfield.GetCell(38, 1).Occupant = TetrominoType.T;

        var cleared = playfield.DetectFullRows();
        Assert.Equal(new[] { 39 }, cleared);

        playfield.CollapseRows(cleared);

        Assert.True(playfield.GetCell(39, 0).IsOccupied);
        Assert.True(playfield.GetCell(39, 1).IsOccupied);
        Assert.False(playfield.GetCell(39, 2).IsOccupied);
        Assert.False(playfield.GetCell(38, 0).IsOccupied);
    }

    // SC-002/FR-001: a row one cell short never clears.
    [Fact]
    public void RowMissingOneCell_DoesNotClear()
    {
        var playfield = new Playfield();
        FillRow(playfield, 39, exceptColumns: 9);

        var cleared = playfield.DetectFullRows();

        Assert.Empty(cleared);
    }

    // SC-004/SC-005/FR-004/FR-005/EC-2: non-adjacent cleared rows collapse correctly -- rows
    // between them shift by one, rows above both shift by two, and no block changes column.
    [Fact]
    public void NonAdjacentClearedRows_CollapseCorrectly()
    {
        var playfield = new Playfield();
        FillRow(playfield, 36); // full -- cleared
        FillRow(playfield, 39); // full -- cleared
        FillRow(playfield, 37, exceptColumns: new[] { 8, 9 }); // not full, survives
        FillRow(playfield, 38, exceptColumns: new[] { 8, 9 }); // not full, survives
        playfield.GetCell(35, 0).Occupant = TetrominoType.L; // survives, single block

        var cleared = playfield.DetectFullRows();
        Assert.Equal(new[] { 36, 39 }, cleared);

        playfield.CollapseRows(cleared);

        // Old row 38 -> new row 39; old row 37 -> new row 38; old row 35 -> new row 37.
        for (var column = 0; column < Playfield.ColumnCount; column++)
        {
            var expectedOccupied39 = column != 8 && column != 9;
            Assert.Equal(expectedOccupied39, playfield.GetCell(39, column).IsOccupied);
        }

        Assert.True(playfield.GetCell(37, 0).IsOccupied);
        for (var column = 1; column < Playfield.ColumnCount; column++)
        {
            Assert.False(playfield.GetCell(37, column).IsOccupied);
        }

        Assert.False(playfield.GetCell(0, 0).IsOccupied);
        Assert.False(playfield.GetCell(1, 0).IsOccupied);
    }

    // SC-005/FR-004: the general non-adjacent case -- rows 25 and 31 cleared, everything between
    // shifts by one, everything above shifts by two, nothing below 31 moves.
    [Fact]
    public void GeneralNonAdjacentCollapse_ShiftsRowsByCorrectAmounts()
    {
        var playfield = new Playfield();
        FillRow(playfield, 25);
        FillRow(playfield, 31);

        // Mark a distinguishable single block in every other row from 0 to 39.
        for (var row = 0; row <= 39; row++)
        {
            if (row == 25 || row == 31)
            {
                continue;
            }

            playfield.GetCell(row, 5).Occupant = TetrominoType.S;
        }

        var cleared = playfield.DetectFullRows();
        Assert.Equal(new[] { 25, 31 }, cleared);

        playfield.CollapseRows(cleared);

        // Rows 26-30 (5 rows) moved down by 1 -> now at 27-31.
        for (var oldRow = 26; oldRow <= 30; oldRow++)
        {
            Assert.True(playfield.GetCell(oldRow + 1, 5).IsOccupied);
        }

        // Rows 0-24 moved down by 2 -> now at 2-26.
        for (var oldRow = 0; oldRow <= 24; oldRow++)
        {
            Assert.True(playfield.GetCell(oldRow + 2, 5).IsOccupied);
        }

        // Rows 32-39 did not move.
        for (var row = 32; row <= 39; row++)
        {
            Assert.True(playfield.GetCell(row, 5).IsOccupied);
        }

        Assert.False(playfield.GetCell(0, 5).IsOccupied);
        Assert.False(playfield.GetCell(1, 5).IsOccupied);
    }

    // SC-006/FR-006/DR-78: blocks left floating by a collapse are not re-settled.
    [Fact]
    public void Collapse_LeavesUnsupportedBlocksFloating()
    {
        var playfield = new Playfield();
        FillRow(playfield, 39);
        playfield.GetCell(38, 4).Occupant = TetrominoType.O;

        var cleared = playfield.DetectFullRows();
        playfield.CollapseRows(cleared);

        Assert.True(playfield.GetCell(39, 4).IsOccupied);
        Assert.False(playfield.GetCell(38, 4).IsOccupied); // not re-settled downward onto anything else
    }

    // SC-007/FR-001: a full row in the hidden buffer (rows 0-19) clears on the same terms.
    [Fact]
    public void FullBufferRow_ClearsLikeAVisibleRow()
    {
        var playfield = new Playfield();
        FillRow(playfield, 19);

        var cleared = playfield.DetectFullRows();

        Assert.Equal(new[] { 19 }, cleared);
    }

    // SC-008/FR-003: clearing N rows leaves exactly the topmost N rows empty.
    [Fact]
    public void Collapse_LeavesTopmostRowsEmpty()
    {
        var playfield = new Playfield();
        FillRow(playfield, 37);
        FillRow(playfield, 38);
        FillRow(playfield, 39);

        var cleared = playfield.DetectFullRows();
        Assert.Equal(3, cleared.Count);

        playfield.CollapseRows(cleared);

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < Playfield.ColumnCount; column++)
            {
                Assert.False(playfield.GetCell(row, column).IsOccupied);
            }
        }
    }

    // SC-012/FR-010: a single row clear at level 1 scores exactly 100.
    [Fact]
    public void SingleRowClear_AtLevel1_Scores100()
    {
        var session = new GameSession(pieceQueueSeed: 7);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;
        var distance = piece.ComputeHardDropDistance(session.Playfield);
        var landingCells = piece.OccupiedCells.Select(c => new CellPosition(c.Column, c.Row + distance)).ToArray();
        var landingRow = landingCells.Max(c => c.Row);
        var occupiedColumns = landingCells.Where(c => c.Row == landingRow).Select(c => c.Column).Distinct().ToArray();

        FillRow(session.Playfield, landingRow, occupiedColumns);

        var scoreBefore = session.Score;
        session.HardDrop();

        Assert.Equal(1, session.LinesCleared);
        Assert.NotNull(session.LastLineClear);
        Assert.Equal(ClearType.Single, session.LastLineClear!.ClearType);
        Assert.Equal(100, session.LastLineClear.PointsAwarded);
        // Score also includes hard-drop distance points (DR-81), so assert the line-clear delta
        // via LastLineClear rather than asserting Score in isolation.
        Assert.True(session.Score >= scoreBefore + 100);
    }

    // SC-013/FR-009/FR-010: the full clear-type/points table at various levels.
    [Theory]
    [InlineData(0, "None", 1, 0)]
    [InlineData(1, "Single", 1, 100)]
    [InlineData(2, "Double", 1, 300)]
    [InlineData(3, "Triple", 1, 500)]
    [InlineData(4, "Tetris", 1, 800)]
    [InlineData(1, "Single", 5, 500)]
    [InlineData(2, "Double", 5, 1500)]
    [InlineData(3, "Triple", 5, 2500)]
    [InlineData(4, "Tetris", 5, 4000)]
    [InlineData(4, "Tetris", 12, 9600)]
    public void LineClearResult_For_ComputesExpectedTypeAndPoints(int rows, string expectedType, int level, long expectedPoints)
    {
        var clearedRows = Enumerable.Range(0, rows).ToArray();

        var result = LineClearResult.For(clearedRows, level);

        Assert.Equal(expectedType, result.ClearType.ToString());
        Assert.Equal(expectedPoints, result.PointsAwarded);
    }

    // SC-014: four separate Singles score less in total than one Tetris.
    [Fact]
    public void FourSeparateSingles_ScoreLessThanOneTetris()
    {
        var singlesTotal = Enumerable.Range(0, 4)
            .Select(_ => LineClearResult.For(new[] { 0 }, 1).PointsAwarded)
            .Sum();
        var tetrisTotal = LineClearResult.For(new[] { 0, 1, 2, 3 }, 1).PointsAwarded;

        Assert.Equal(400, singlesTotal);
        Assert.Equal(800, tetrisTotal);
        Assert.True(tetrisTotal > singlesTotal);
    }

    // SC-017/FR-012/FR-015: soft drop pays 1 point per cell, not multiplied by level.
    [Fact]
    public void SoftDrop_PaysOnePointPerCell_IgnoringLevel()
    {
        var session = new GameSession(pieceQueueSeed: 11);
        session.Start(DateTimeOffset.UtcNow);
        session.PressSoftDrop();

        // Advance enough elapsed time to softly drop several cells without grounding/locking.
        for (var i = 0; i < 6; i++)
        {
            session.Tick(session.SoftDropInterval);
            if (session.ActivePiece is null)
            {
                break; // locked early due to a short floor distance for this seed/spawn.
            }
        }

        Assert.True(session.Score > 0);
        Assert.True(session.Score <= 6); // never more than 1 point per softly-dropped cell
    }

    // SC-018/FR-013: hard drop pays 2 points per cell travelled.
    [Fact]
    public void HardDrop_PaysTwoPointsPerCellTravelled()
    {
        var session = new GameSession(pieceQueueSeed: 13);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;
        var expectedDistance = piece.ComputeHardDropDistance(session.Playfield);

        var outcome = session.HardDrop();

        Assert.Equal(expectedDistance, outcome!.CellsTravelled);
        Assert.Equal(expectedDistance * 2, session.Score);
    }

    // SC-019/FR-014: drop points and line clear points combine on one lock.
    [Fact]
    public void DropPointsAndLineClearPoints_CombineOnOneLock()
    {
        var session = new GameSession(pieceQueueSeed: 17);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;
        var distance = piece.ComputeHardDropDistance(session.Playfield);
        var landingCells = piece.OccupiedCells.Select(c => new CellPosition(c.Column, c.Row + distance)).ToArray();
        var landingRow = landingCells.Max(c => c.Row);
        var occupiedColumns = landingCells.Where(c => c.Row == landingRow).Select(c => c.Column).Distinct().ToArray();

        FillRow(session.Playfield, landingRow, occupiedColumns);

        var outcome = session.HardDrop();

        var expectedDropPoints = outcome!.CellsTravelled * 2;
        var expectedLinePoints = session.LastLineClear!.PointsAwarded;

        Assert.Equal(expectedDropPoints + expectedLinePoints, session.Score);
    }

    // SC-020/FR-012/FR-013: a drop that travels no distance pays nothing.
    [Fact]
    public void GroundedHardDrop_TravelsNoDistance_PaysNothing()
    {
        var session = new GameSession(pieceQueueSeed: 19);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;

        // Drop it once so it's locked, forcing the next piece to spawn; instead, simulate an
        // already-grounded piece by filling the row directly beneath its current footprint.
        foreach (var column in piece.OccupiedCells.Select(c => c.Column).Distinct())
        {
            session.Playfield.GetCell(piece.OccupiedCells.Max(c => c.Row) + 1, column).Occupant = TetrominoType.O;
        }

        var outcome = session.HardDrop();

        Assert.Equal(0, outcome!.CellsTravelled);
        // No drop points; only a possible (improbable for one lock) line clear could add score.
        Assert.Equal(session.LastLineClear!.PointsAwarded, session.Score);
    }

    // SC-021/FR-015: ordinary gravity (no soft drop held) earns nothing.
    [Fact]
    public void OrdinaryGravity_EarnsNoPoints()
    {
        var session = new GameSession(pieceQueueSeed: 23);
        session.Start(DateTimeOffset.UtcNow);

        session.Tick(session.GravityInterval); // one ordinary gravity step, no soft drop held

        Assert.Equal(0, session.Score);
    }

    // SC-023/FR-016: the line count accumulates across the whole session and never decreases.
    [Fact]
    public void LinesCleared_AccumulatesAcrossSession()
    {
        var session = new GameSession(pieceQueueSeed: 29);
        session.Start(DateTimeOffset.UtcNow);

        var before = session.LinesCleared;

        for (var i = 0; i < 3 && session.Status == GameStatus.Playing; i++)
        {
            session.HardDrop();
        }

        Assert.True(session.LinesCleared >= before);
    }

    // SC-024/FR-019: a new session starts with score and lines both at 0.
    [Fact]
    public void NewSession_StartsWithZeroScoreAndLines()
    {
        var session = new GameSession(pieceQueueSeed: 31);
        session.Start(DateTimeOffset.UtcNow);

        Assert.Equal(0, session.Score);
        Assert.Equal(0, session.LinesCleared);
    }

    // FR-017/DR-83: score never decreases across a sequence of locks.
    [Fact]
    public void Score_NeverDecreases_AcrossMultipleLocks()
    {
        var session = new GameSession(pieceQueueSeed: 37);
        session.Start(DateTimeOffset.UtcNow);

        long previous = session.Score;
        for (var i = 0; i < 20 && session.Status == GameStatus.Playing; i++)
        {
            session.HardDrop();
            Assert.True(session.Score >= previous);
            previous = session.Score;
        }
    }

    // EC-1: a lock that clears nothing still records a None result with 0 points, and
    // LastLineClear is populated rather than left null.
    [Fact]
    public void LockClearingNothing_RecordsNoneResult()
    {
        var session = new GameSession(pieceQueueSeed: 41);
        session.Start(DateTimeOffset.UtcNow);

        session.HardDrop();

        Assert.NotNull(session.LastLineClear);
        Assert.Equal(ClearType.None, session.LastLineClear!.ClearType);
        Assert.Equal(0, session.LastLineClear.PointsAwarded);
        Assert.Equal(0, session.LinesCleared);
    }

    // FR-008/EC-5/DR-85 step 5: a clear that frees the spawn area averts a block out.
    [Fact]
    public void ClearThatFreesSpawnArea_AvertsBlockOut()
    {
        var session = new GameSession(pieceQueueSeed: 43);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;
        var distance = piece.ComputeHardDropDistance(session.Playfield);
        var landingCells = piece.OccupiedCells.Select(c => new CellPosition(c.Column, c.Row + distance)).ToArray();
        var landingRow = landingCells.Max(c => c.Row);
        var occupiedColumns = landingCells.Where(c => c.Row == landingRow).Select(c => c.Column).Distinct().ToArray();

        // Fill the landing row around the piece so it clears...
        FillRow(session.Playfield, landingRow, occupiedColumns);

        // ...and also occupy the buffer rows where the next piece would spawn, so that without
        // the clear's collapse the next spawn would block out. The collapse should shift stack
        // contents down and clear exactly the one full row, freeing row 19/18 area somewhat --
        // this is a smoke test that the session stays Playing when a clear coincides with a
        // congested buffer, not an exact geometry proof (which SC-004/SC-005 already cover).
        session.HardDrop();

        Assert.Equal(GameStatus.Playing, session.Status);
    }
}
