using System.Linq;
using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for US-004 (docs/specs/001-start-new-game.md), covering SC-017..SC-024 and edge cases
/// EC-5/EC-6. All timing is driven through <see cref="GameSession.Tick"/> with simulated deltas
/// (NFR-006) — no real elapsed wall-clock time is used anywhere in this file.
/// </summary>
public class LockDelayTests
{
    /// <summary>Forces the session's active piece down to the floor row (row 39), grounded.</summary>
    private static ActivePiece DropActivePieceToFloor(GameSession session)
    {
        var piece = session.ActivePiece!;
        var maxRowOffset = TetrominoDefinition.For(piece.Type).SpawnOffsets.Max(o => o.RowOffset);
        piece.Position = new CellPosition(piece.Position.Column, 39 - maxRowOffset);
        Assert.True(piece.IsGrounded(session.Playfield));
        return piece;
    }

    /// <summary>
    /// Feeds <paramref name="elapsed"/> to <see cref="GameSession.Tick"/> as a series of calls
    /// no larger than <see cref="GameSession.MaxElapsedPerTick"/> (100ms), since DR-26/EC-2 caps
    /// every single call regardless of the delta passed to it.
    /// </summary>
    private static void TickOverMultipleCalls(GameSession session, TimeSpan elapsed)
    {
        var remaining = elapsed;
        var step = GameSession.MaxElapsedPerTick;
        while (remaining > TimeSpan.Zero)
        {
            var thisStep = remaining > step ? step : remaining;
            session.Tick(thisStep);
            remaining -= thisStep;
        }
    }

    // SC-017: A piece grounded on the floor locks after the delay (FR-016, FR-019). Also covers
    // EC-5 (floor row locks like any other grounded piece, no special case).
    [Fact]
    public void Tick_GroundedOnFloor_LocksAfterHalfASecond()
    {
        var session = new GameSession(pieceQueueSeed: 1);
        session.Start(DateTimeOffset.UtcNow);
        var piece = DropActivePieceToFloor(session);
        var occupiedCells = piece.OccupiedCells.ToArray();

        // Just under 0.500s: not yet locked.
        for (var i = 0; i < 4; i++)
        {
            session.Tick(TimeSpan.FromMilliseconds(100));
        }
        Assert.NotNull(session.ActivePiece);
        Assert.Same(piece, session.ActivePiece);
        foreach (var cell in occupiedCells)
        {
            Assert.False(session.Playfield.GetCell(cell.Row, cell.Column).IsOccupied);
        }

        // The remaining 100ms crosses the 0.500s threshold.
        session.Tick(TimeSpan.FromMilliseconds(100));

        // The piece that was on the floor is no longer active (a new one has spawned, DR-20),
        // and its four cells are now occupied cells of its type.
        Assert.NotSame(piece, session.ActivePiece);
        foreach (var cell in occupiedCells)
        {
            Assert.Equal(piece.Type, session.Playfield.GetCell(cell.Row, cell.Column).Occupant);
        }
    }

    // SC-018: A piece grounded on the stack locks after the delay (FR-016, FR-019).
    [Fact]
    public void Tick_GroundedOnStack_LocksAfterHalfASecond()
    {
        var session = new GameSession(pieceQueueSeed: 6);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;

        // Build a stack directly beneath the piece's current cells so it becomes grounded
        // without reaching the floor.
        foreach (var cell in piece.OccupiedCells)
        {
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }
        Assert.True(piece.IsGrounded(session.Playfield));
        var occupiedCells = piece.OccupiedCells.ToArray();

        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(500));

        Assert.NotSame(piece, session.ActivePiece);
        foreach (var cell in occupiedCells)
        {
            Assert.Equal(piece.Type, session.Playfield.GetCell(cell.Row, cell.Column).Occupant);
        }
    }

    // SC-019: A piece that can fall again does not lock (FR-017, DR-18).
    [Fact]
    public void Tick_BecomesUngroundedBeforeDelayExpires_CancelsLockTimerAndResumesGravity()
    {
        var session = new GameSession(pieceQueueSeed: 9);
        session.Start(DateTimeOffset.UtcNow);
        var piece = DropActivePieceToFloor(session);
        var rowOnFloor = piece.Position.Row;

        // Run the lock delay partway.
        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(300));
        Assert.NotNull(piece.LockTimerElapsed);

        // The piece becomes un-grounded again (simulating story 002 moving it over a gap, or
        // here simply lifting it back up so there is free space beneath it).
        piece.Position = new CellPosition(piece.Position.Column, rowOnFloor - 5);
        Assert.False(piece.IsGrounded(session.Playfield));

        session.Tick(TimeSpan.FromMilliseconds(100));

        // Lock timer cancelled (DR-18) -- the piece is still active, not locked.
        Assert.Null(piece.LockTimerElapsed);
        Assert.Same(piece, session.ActivePiece);

        // Gravity has resumed: running the full remaining lock-delay window as gravity ticks
        // must not lock the piece while it's not grounded.
        for (var i = 0; i < 4; i++)
        {
            session.Tick(TimeSpan.FromMilliseconds(100));
        }
        Assert.Same(piece, session.ActivePiece);
    }

    // SC-020: The reset budget is bounded at 15 (FR-018, EC-4).
    [Fact]
    public void TryResetLockTimer_ExhaustedBudget_GrantsNoFurtherResets_PieceLocksOnSchedule()
    {
        var session = new GameSession(pieceQueueSeed: 11);
        session.Start(DateTimeOffset.UtcNow);
        var piece = DropActivePieceToFloor(session);

        session.Tick(TimeSpan.FromMilliseconds(10)); // start the lock delay running

        for (var i = 0; i < ActivePiece.MaxLockResets; i++)
        {
            Assert.True(piece.TryResetLockTimer());
        }
        Assert.Equal(ActivePiece.MaxLockResets, piece.LockResetsUsed);

        // Budget exhausted: a further reset is refused.
        Assert.False(piece.TryResetLockTimer());

        // The piece locks when the running 0.500s delay expires, unaffected by the refusal.
        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(500));
        Assert.NotSame(piece, session.ActivePiece);
    }

    // SC-021: Falling to a new low row replenishes the budget (FR-018).
    [Fact]
    public void Tick_NewLowestRow_ReplenishesResetBudgetToMax()
    {
        var session = new GameSession(pieceQueueSeed: 13);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;

        // Ground the piece where it currently is, consume some of its budget.
        foreach (var cell in piece.OccupiedCells)
        {
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }
        session.Tick(TimeSpan.FromMilliseconds(10));
        Assert.True(piece.TryResetLockTimer());
        Assert.True(piece.TryResetLockTimer());
        Assert.Equal(2, piece.LockResetsUsed);

        // Clear the stack beneath it and let it fall to a new, lower row.
        foreach (var cell in piece.OccupiedCells)
        {
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = null;
        }
        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(1000)); // falls one cell past its previous lowest row

        Assert.Equal(0, piece.LockResetsUsed);
    }

    // SC-022: The next piece follows immediately (FR-020).
    [Fact]
    public void Tick_AfterLock_SpawnsNextPieceImmediately_QueueAdvancesByOne()
    {
        var session = new GameSession(pieceQueueSeed: 17);
        session.Start(DateTimeOffset.UtcNow);
        var firstPiece = DropActivePieceToFloor(session);
        var upcomingBeforeLock = session.PieceQueue.Upcoming.ToArray();
        var spawnedBeforeLock = session.PiecesSpawned;

        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(500));

        Assert.Equal(GameStatus.Playing, session.Status);
        Assert.NotNull(session.ActivePiece);
        Assert.NotSame(firstPiece, session.ActivePiece);
        Assert.Equal(spawnedBeforeLock + 1, session.PiecesSpawned);
        // The next piece spawned is exactly what was at the front of the queue before the lock.
        Assert.Equal(upcomingBeforeLock[0], session.ActivePiece!.Type);
    }

    // SC-023: A blocked spawn ends the session (FR-021, EC-1).
    [Fact]
    public void Lock_SpawnBlockedByOccupiedStack_MovesToGameOver_PieceNotPlaced()
    {
        var session = new GameSession(pieceQueueSeed: 19);
        session.Start(DateTimeOffset.UtcNow);
        var piece = DropActivePieceToFloor(session);

        // Occupy the spawn placement for whatever type is next in the queue, so that once this
        // piece locks and Spawn() is attempted again, it finds its spawn cells already taken.
        var nextType = session.PieceQueue.Upcoming[0];
        var nextDefinition = TetrominoDefinition.For(nextType);
        var minColumnOffset = nextDefinition.SpawnOffsets.Min(o => o.ColumnOffset);
        var maxRowOffset = nextDefinition.SpawnOffsets.Max(o => o.RowOffset);
        var spawnColumn = (Playfield.ColumnCount - nextDefinition.SpawnWidth) / 2 - minColumnOffset;
        var spawnRow = 19 - maxRowOffset;
        foreach (var offset in nextDefinition.SpawnOffsets)
        {
            session.Playfield.GetCell(spawnRow + offset.RowOffset, spawnColumn + offset.ColumnOffset).Occupant = TetrominoType.O;
        }

        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(500)); // locks the floor piece, then tries to spawn

        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.Null(session.ActivePiece);
    }

    // SC-024: A piece locking entirely above the visible field ends the session (FR-022, EC-6).
    [Fact]
    public void Lock_AllFourCellsInBufferRows_WritesCellsThenMovesToGameOver()
    {
        var session = new GameSession(pieceQueueSeed: 23);
        session.Start(DateTimeOffset.UtcNow);

        // Replace whatever spawned (which may already have stepped into the visible field per
        // DR-08) with a piece deliberately placed so all four of its cells sit in buffer rows
        // (row <= 19) -- the unstepped T spawn placement from the spec's SpawnPlacement table.
        var piece = new ActivePiece
        {
            Type = TetrominoType.T,
            Orientation = Orientation.Spawn,
            Position = new CellPosition(3, 18)
        };
        session.ActivePiece = piece;

        // Occupy the row directly beneath the piece's current cells so it becomes grounded in
        // place without ever entering the visible field.
        foreach (var cell in piece.OccupiedCells)
        {
            Assert.True(cell.Row <= 19);
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }
        var occupiedCells = piece.OccupiedCells.ToArray();
        Assert.True(piece.IsGrounded(session.Playfield));

        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(500));

        foreach (var cell in occupiedCells)
        {
            Assert.Equal(piece.Type, session.Playfield.GetCell(cell.Row, cell.Column).Occupant);
        }
        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.Null(session.ActivePiece);
    }

    // DR-27: once GameOver, Tick is a no-op -- no further spawn, gravity or lock.
    [Fact]
    public void Tick_AfterGameOver_IsANoOp()
    {
        var session = new GameSession(pieceQueueSeed: 29);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;
        foreach (var cell in piece.OccupiedCells)
        {
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }
        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(500));
        Assert.Equal(GameStatus.GameOver, session.Status);

        var spawnedBefore = session.PiecesSpawned;
        session.Tick(TimeSpan.FromSeconds(10));

        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.Equal(spawnedBefore, session.PiecesSpawned);
        Assert.Null(session.ActivePiece);
    }
}
