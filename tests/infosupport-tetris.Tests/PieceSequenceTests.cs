using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for US-002 (docs/specs/001-start-new-game.md), covering SC-006..SC-014: the seven
/// tetromino shapes, the 7-bag randomiser, queue refill, spawn placement, the DR-08 initial
/// step and seed reproducibility.
/// </summary>
public class PieceSequenceTests
{
    // SC-006: Every piece is a four-cell tetromino (FR-006).
    [Theory]
    [InlineData(TetrominoType.I)]
    [InlineData(TetrominoType.O)]
    [InlineData(TetrominoType.T)]
    [InlineData(TetrominoType.S)]
    [InlineData(TetrominoType.Z)]
    [InlineData(TetrominoType.J)]
    [InlineData(TetrominoType.L)]
    public void EveryTetromino_OccupiesExactlyFourCells_InSpawnOrientation(TetrominoType type)
    {
        var definition = TetrominoDefinition.For(type);
        Assert.Equal(4, definition.SpawnOffsets.Length);

        var session = new GameSession(pieceQueueSeed: 1);
        session.Start(DateTimeOffset.UtcNow);
        // Spawn already ran once in Start(); spawn again of our own accord via a fresh session
        // per type isn't necessary - OccupiedCells derives directly from the definition, so
        // we can assert shape correctness straight from the definition and spawn placement.
        var piece = new ActivePiece { Type = type, Position = new CellPosition(0, 0) };
        Assert.Equal(4, piece.OccupiedCells.Count);
        Assert.Contains(type, Enum.GetValues<TetrominoType>());
    }

    // SC-007: Each bag contains all seven pieces exactly once (FR-008).
    [Fact]
    public void FirstSevenDraws_AreAllSevenTypes_WithNoRepeats()
    {
        var queue = new PieceQueue(seed: 42);
        var drawn = new List<TetrominoType>();
        for (var i = 0; i < 7; i++)
        {
            drawn.Add(queue.Draw());
        }

        Assert.Equal(7, drawn.Distinct().Count());
        Assert.Equal(Enum.GetValues<TetrominoType>().OrderBy(t => t), drawn.OrderBy(t => t));
    }

    // SC-008: The sequence is fair across bag boundaries (FR-008).
    [Fact]
    public void SeventyConsecutiveDraws_EachTypeExactlyTenTimes_NoLongRepeatsOrDroughts()
    {
        var queue = new PieceQueue(seed: 7);
        var drawn = new List<TetrominoType>();
        for (var i = 0; i < 70; i++)
        {
            drawn.Add(queue.Draw());
        }

        foreach (var type in Enum.GetValues<TetrominoType>())
        {
            Assert.Equal(10, drawn.Count(t => t == type));
        }

        // No type appears more than twice consecutively (DR-12).
        var run = 1;
        for (var i = 1; i < drawn.Count; i++)
        {
            run = drawn[i] == drawn[i - 1] ? run + 1 : 1;
            Assert.True(run <= 2, $"Type {drawn[i]} repeated {run} times consecutively at index {i}.");
        }

        // No more than 12 pieces separate two consecutive occurrences of the same type (DR-11).
        foreach (var type in Enum.GetValues<TetrominoType>())
        {
            var indices = drawn.Select((t, idx) => (t, idx)).Where(x => x.t == type).Select(x => x.idx).ToList();
            for (var i = 1; i < indices.Count; i++)
            {
                Assert.True(indices[i] - indices[i - 1] - 1 <= 12,
                    $"Gap between occurrences of {type} exceeds 12 at indices {indices[i - 1]},{indices[i]}.");
            }
        }
    }

    // SC-009: The queue stays stocked for look-ahead (FR-009).
    [Fact]
    public void QueueAlwaysHoldsAtLeastSevenUpcoming_AfterADraw()
    {
        var queue = new PieceQueue(seed: 3);
        for (var i = 0; i < 30; i++)
        {
            queue.Draw();
            Assert.True(queue.Upcoming.Count >= 7);
        }
    }

    // SC-010: Pieces spawn centred, upright, just above the visible field (FR-010).
    [Fact]
    public void FirstPiece_Spawns_InSpawnOrientation_CenteredWithLeftBias()
    {
        var session = new GameSession(pieceQueueSeed: 11);
        session.Start(DateTimeOffset.UtcNow);

        var piece = session.ActivePiece;
        Assert.NotNull(piece);
        Assert.Equal(Orientation.Spawn, piece!.Orientation);

        var definition = TetrominoDefinition.For(piece.Type);
        var expectedColumn = (Playfield.ColumnCount - definition.SpawnWidth) / 2;
        var leftmostColumn = piece.OccupiedCells.Min(c => c.Column);
        Assert.Equal(expectedColumn, leftmostColumn);
    }

    // SC-011: Spawn placement for each of the seven types (FR-010).
    [Theory]
    [InlineData(TetrominoType.I, 3, 4, 5, 6)]
    [InlineData(TetrominoType.O, 4, 5)]
    [InlineData(TetrominoType.T, 3, 4, 5)]
    [InlineData(TetrominoType.S, 3, 4, 5)]
    [InlineData(TetrominoType.Z, 3, 4, 5)]
    [InlineData(TetrominoType.J, 3, 4, 5)]
    [InlineData(TetrominoType.L, 3, 4, 5)]
    public void SpawnPlacement_OccupiesExpectedColumns(TetrominoType type, params int[] expectedColumns)
    {
        var definition = TetrominoDefinition.For(type);
        var minColumnOffset = definition.SpawnOffsets.Min(o => o.ColumnOffset);
        var maxRowOffset = definition.SpawnOffsets.Max(o => o.RowOffset);
        var spawnColumn = (Playfield.ColumnCount - definition.SpawnWidth) / 2 - minColumnOffset;
        var spawnRow = 19 - maxRowOffset;
        var piece = new ActivePiece { Type = type, Position = new CellPosition(spawnColumn, spawnRow) };

        var occupiedColumns = piece.OccupiedCells.Select(c => c.Column).Distinct().OrderBy(c => c);
        Assert.Equal(expectedColumns.OrderBy(c => c), occupiedColumns);

        // Lowest occupied cell is row 19, the bottom-most buffer row (DR-06).
        Assert.Equal(19, piece.OccupiedCells.Max(c => c.Row));
    }

    // SC-012: A spawning piece steps into view straight away (FR-011).
    [Fact]
    public void Spawn_StepsDownOneCell_WhenFree_SoAtLeastOneCellEntersTheTopVisibleRow()
    {
        var session = new GameSession(pieceQueueSeed: 5);
        session.Start(DateTimeOffset.UtcNow);

        var piece = session.ActivePiece;
        Assert.NotNull(piece);
        // After the free initial step, the piece's lowest occupied row is 20 (top visible row),
        // one below the pre-step row 19.
        Assert.Equal(20, piece!.OccupiedCells.Max(c => c.Row));
        Assert.Contains(piece.OccupiedCells, c => c.Row == Playfield.BufferRowCount);
    }

    // SC-013: The initial step is skipped when blocked (FR-011, FR-015).
    [Fact]
    public void Spawn_SkipsInitialStep_WhenTheCellsBelowAreOccupied()
    {
        var session = new GameSession(pieceQueueSeed: 5);

        // Determine what the first piece will be without consuming it from the real queue,
        // then pre-occupy the row directly beneath its spawn cells so the one-cell descent is
        // blocked (DR-04), per SC-013's "stack reaches the top visible row directly beneath the
        // spawn position".
        var previewQueue = new PieceQueue(seed: 5);
        var firstType = previewQueue.Upcoming[0];
        var definition = TetrominoDefinition.For(firstType);
        var minColumnOffset = definition.SpawnOffsets.Min(o => o.ColumnOffset);
        var maxRowOffset = definition.SpawnOffsets.Max(o => o.RowOffset);
        var spawnColumn = (Playfield.ColumnCount - definition.SpawnWidth) / 2 - minColumnOffset;
        var spawnRow = 19 - maxRowOffset;

        // Occupy exactly the cells one row below each of the piece's spawn cells, so its spawn
        // cells themselves are free but the one-cell descent (DR-08) is blocked (DR-04).
        foreach (var offset in definition.SpawnOffsets)
        {
            var column = spawnColumn + offset.ColumnOffset;
            var row = spawnRow + offset.RowOffset + 1;
            session.Playfield.GetCell(row, column).Occupant = TetrominoType.O;
        }

        session.Start(DateTimeOffset.UtcNow);

        var piece = session.ActivePiece;
        Assert.NotNull(piece);
        // Remains in its spawn position: lowest occupied cell still row 19.
        Assert.Equal(19, piece!.OccupiedCells.Max(c => c.Row));

        // Grounded: moving down one more cell would be an invalid placement.
        var steppedCells = piece.OccupiedCells.Select(c => new CellPosition(c.Column, c.Row + 1));
        Assert.False(session.Playfield.IsValidPlacement(steppedCells));
    }

    // SC-014: The same seed gives the same game (FR-012).
    [Fact]
    public void SameSeed_ProducesIdenticalFiftyPieceSequences()
    {
        var queueA = new PieceQueue(seed: 99);
        var queueB = new PieceQueue(seed: 99);

        var sequenceA = Enumerable.Range(0, 50).Select(_ => queueA.Draw()).ToList();
        var sequenceB = Enumerable.Range(0, 50).Select(_ => queueB.Draw()).ToList();

        Assert.Equal(sequenceA, sequenceB);
    }
}
