using System.Linq;
using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for US-004... docs/specs/004-next-piece-preview.md. Covers the §4 acceptance scenarios
/// that are expressible headlessly (NFR-007) without rasterising pixels.
/// </summary>
public class NextPiecePreviewTests
{
    // SC-002/EC-1 (FR-006): populated before the first piece ever spawns.
    [Fact]
    public void Preview_IsPopulated_BeforeFirstSpawn_WhileStillReady()
    {
        var session = new GameSession(pieceQueueSeed: 42);
        var expected = session.PieceQueue.Upcoming[0];

        var preview = session.NextPiecePreview;

        Assert.Equal(GameStatus.Ready, session.Status);
        Assert.Equal(expected, preview.PreviewedType);
    }

    // SC-001 (FR-001/FR-002/FR-003): shows the front of the queue, in Spawn orientation and the
    // DR-24 colour.
    [Fact]
    public void Preview_ShowsFrontOfQueue_InSpawnOrientation_WithTheTypesColour()
    {
        var session = new GameSession(pieceQueueSeed: 7);
        var expectedType = session.PieceQueue.Upcoming[0];
        var expectedColour = TetrominoDefinition.For(expectedType).Colour;

        var preview = session.NextPiecePreview;

        Assert.Equal(expectedType, preview.PreviewedType);
        Assert.Equal(Orientation.Spawn, preview.Orientation);
        Assert.Equal(expectedColour, preview.Colour);
        Assert.Equal(4, preview.Cells.Count);
    }

    // SC-004/SC-008 (FR-009/DR-55): what was previewed is exactly what spawns, across many draws,
    // including repeats at a bag boundary.
    [Fact]
    public void Preview_MatchesTheTypeThatSpawnsNext_Across200Spawns()
    {
        var session = new GameSession(pieceQueueSeed: 123);

        for (var i = 0; i < 200 && session.Status != GameStatus.GameOver; i++)
        {
            var previewedBeforeSpawn = session.NextPiecePreview.PreviewedType;

            session.ActivePiece = null; // directly trigger the next draw point (DR-54), bypassing timing
            session.Spawn();

            if (session.ActivePiece is not null)
            {
                Assert.Equal(previewedBeforeSpawn, session.ActivePiece.Type);
            }
        }
    }

    // SC-005 (FR-008/DR-53): reading the preview never mutates the queue.
    [Fact]
    public void Preview_NeverMutatesTheQueue_SameSeedProducesIdenticalSequences()
    {
        var withPreview = new GameSession(pieceQueueSeed: 99);
        var withoutPreview = new GameSession(pieceQueueSeed: 99);

        var drawnWith = new System.Collections.Generic.List<TetrominoType>();
        var drawnWithout = new System.Collections.Generic.List<TetrominoType>();

        for (var i = 0; i < 50; i++)
        {
            // Reading the preview repeatedly must not change what Draw() yields next.
            _ = withPreview.NextPiecePreview;
            _ = withPreview.NextPiecePreview;
            drawnWith.Add(withPreview.PieceQueue.Draw());

            drawnWithout.Add(withoutPreview.PieceQueue.Draw());
        }

        Assert.Equal(drawnWithout, drawnWith);
        Assert.True(withPreview.PieceQueue.Upcoming.Count >= 7);
    }

    // SC-011/DR-60: the preview empties at game over but the grid stays reserved.
    [Fact]
    public void Preview_EmptiesAtGameOver_ButKeepsTheGridReserved()
    {
        var session = new GameSession(pieceQueueSeed: 5);
        session.Start(DateTimeOffset.UtcNow);

        // Force a block-out: occupy every spawn cell directly, then try to spawn again.
        for (var row = 0; row <= 19; row++)
        {
            for (var column = 0; column < Playfield.ColumnCount; column++)
            {
                session.Playfield.GetCell(row, column).Occupant = TetrominoType.T;
            }
        }

        session.ActivePiece = null;
        session.Spawn();

        Assert.Equal(GameStatus.GameOver, session.Status);

        var preview = session.NextPiecePreview;

        Assert.Null(preview.PreviewedType);
        Assert.Null(preview.Colour);
        Assert.Empty(preview.Cells);
        Assert.Equal(NextPiecePreview.ColumnCount, 4);
        Assert.Equal(NextPiecePreview.RowCount, 2);
    }

    // SC-006/SC-007/DR-57/DR-58: the preview area is always 4x2 and every type is centred.
    [Theory]
    [InlineData(TetrominoType.I, 4, 0, 0.5)]
    [InlineData(TetrominoType.O, 2, 1, 0)]
    [InlineData(TetrominoType.T, 3, 0.5, 0)]
    [InlineData(TetrominoType.S, 3, 0.5, 0)]
    [InlineData(TetrominoType.Z, 3, 0.5, 0)]
    [InlineData(TetrominoType.J, 3, 0.5, 0)]
    [InlineData(TetrominoType.L, 3, 0.5, 0)]
    public void Preview_CentresEachType_WithinTheFixedFourByTwoGrid(
        TetrominoType type, int occupiedWidth, double expectedHorizontalInset, double expectedVerticalInset)
    {
        // Build a Ready session whose queue front is forced to `type` via direct construction of
        // the expected geometry (we don't need a live session here: the centring math is a pure
        // function of TetrominoDefinition, exercised the same way NextPiecePreview.For does it
        // internally). We assert through a real session instead, to also prove read-only use.
        var definition = TetrominoDefinition.For(type);
        var minColumn = definition.SpawnOffsets.Min(o => o.ColumnOffset);
        var maxColumn = definition.SpawnOffsets.Max(o => o.ColumnOffset);
        Assert.Equal(occupiedWidth, maxColumn - minColumn + 1);

        // Find a seed whose first upcoming piece is `type` (7-bag guarantees one exists within
        // the first bag for some seed; brute-force a small range rather than hard-coding one).
        GameSession? session = null;
        for (var seed = 0; seed < 500; seed++)
        {
            var candidate = new GameSession(seed);
            if (candidate.PieceQueue.Upcoming[0] == type)
            {
                session = candidate;
                break;
            }
        }

        Assert.NotNull(session);
        var preview = session!.NextPiecePreview;

        Assert.Equal(type, preview.PreviewedType);
        Assert.Equal(NextPiecePreview.ColumnCount, 4);
        Assert.Equal(NextPiecePreview.RowCount, 2);

        var minCellColumn = preview.Cells.Min(c => c.Column);
        var minCellRow = preview.Cells.Min(c => c.Row);

        Assert.Equal(expectedHorizontalInset, minCellColumn);
        Assert.Equal(expectedVerticalInset, minCellRow);
    }

    // SC-013/DR-59: a non-colour, non-shape text alternative is available for every type and the
    // empty state.
    [Fact]
    public void Preview_ExposesATextAlternative_ForAPopulatedAndAnEmptyPreview()
    {
        var session = new GameSession(pieceQueueSeed: 3);
        var populated = session.NextPiecePreview;
        Assert.Contains(populated.PreviewedType!.Value.ToString(), populated.AccessibleName);

        session.Start(DateTimeOffset.UtcNow);
        session.ActivePiece = null;
        for (var row = 0; row <= 19; row++)
        {
            for (var column = 0; column < Playfield.ColumnCount; column++)
            {
                session.Playfield.GetCell(row, column).Occupant = TetrominoType.T;
            }
        }
        session.Spawn();

        var empty = session.NextPiecePreview;
        Assert.False(string.IsNullOrWhiteSpace(empty.AccessibleName));
        Assert.Null(empty.PreviewedType);
    }

    // DR-52: only upcoming[0] is ever read, never any other index and never Draw().
    [Fact]
    public void Preview_ReadsOnlyTheFrontOfTheQueue_NeverDrawsFromIt()
    {
        var session = new GameSession(pieceQueueSeed: 11);
        var countBefore = session.PieceQueue.Upcoming.Count;
        var frontBefore = session.PieceQueue.Upcoming[0];

        for (var i = 0; i < 10; i++)
        {
            var preview = session.NextPiecePreview;
            Assert.Equal(frontBefore, preview.PreviewedType);
        }

        Assert.Equal(countBefore, session.PieceQueue.Upcoming.Count);
        Assert.Equal(frontBefore, session.PieceQueue.Upcoming[0]);
    }
}
