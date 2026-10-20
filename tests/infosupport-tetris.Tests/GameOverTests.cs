using System.Linq;
using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for docs/specs/008-game-over.md (US-001..US-005), covering SC-001..SC-018 and the new
/// domain rules DR-100..DR-108. All timing is driven through <see cref="GameSession.Tick"/> with
/// simulated deltas (NFR-007) -- no real elapsed wall-clock time is used anywhere in this file.
/// </summary>
public class GameOverTests
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
    /// Feeds <paramref name="elapsed"/> to <see cref="GameSession.Tick"/> as a series of calls no
    /// larger than <see cref="GameSession.MaxElapsedPerTick"/> (100ms), per DR-26/EC-2.
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

    /// <summary>Drives a session's spawned piece to lock on the floor and returns once it has locked.</summary>
    private static void LockActivePieceOnFloor(GameSession session)
    {
        DropActivePieceToFloor(session);
        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(500));
    }

    // SC-015/DR-103/DR-107/FR-014: a block-out produces exactly one CompletedRun with the right id.
    [Fact]
    public void BlockOut_CreatesCompletedRun_WithFrozenFiguresAndMatchingRunId()
    {
        var session = new GameSession(pieceQueueSeed: 19);
        session.Start(DateTimeOffset.UtcNow);
        LockActivePieceOnFloor(session); // locks the floor piece; a new piece spawns per DR-20.

        // Occupy the spawn placement for whatever type is next, so the subsequent spawn attempt
        // from a second lock finds its spawn cells already taken (block out).
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

        var piecesSpawnedBeforeFinalLock = session.PiecesSpawned;
        LockActivePieceOnFloor(session); // locks, Spawn() attempted, finds spawn blocked.

        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.NotNull(session.CompletedRun);
        var run = session.CompletedRun!;

        Assert.Equal(session.Id, run.RunId);
        Assert.Equal(session.Score, run.FinalScore);
        Assert.Equal(session.LinesCleared, run.LinesCleared);
        Assert.Equal(session.Level, run.FinalLevel);
        Assert.Equal(TopOutReason.BlockOut, run.TopOutReason);

        // DR-108: the piece that failed to spawn by block out never counts.
        Assert.Equal(piecesSpawnedBeforeFinalLock, run.PiecesSpawned);
        Assert.Equal(piecesSpawnedBeforeFinalLock, session.PiecesSpawned);
    }

    // SC-002/SC-015: a lock-out also produces exactly one CompletedRun, and the piece that locked
    // out IS counted towards piecesSpawned (only block-out excludes a piece, per DR-108).
    [Fact]
    public void LockOut_CreatesCompletedRun_WithLockOutReason_AndCountsTheLockedPiece()
    {
        var session = new GameSession(pieceQueueSeed: 23);
        session.Start(DateTimeOffset.UtcNow);

        var piece = new ActivePiece
        {
            Type = TetrominoType.T,
            Orientation = Orientation.Spawn,
            Position = new CellPosition(3, 18)
        };
        session.ActivePiece = piece;

        foreach (var cell in piece.OccupiedCells)
        {
            Assert.True(cell.Row <= 19);
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }
        Assert.True(piece.IsGrounded(session.Playfield));

        var piecesSpawnedBeforeLock = session.PiecesSpawned;
        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(500));

        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.NotNull(session.CompletedRun);
        var run = session.CompletedRun!;

        Assert.Equal(TopOutReason.LockOut, run.TopOutReason);
        Assert.Equal(piecesSpawnedBeforeLock, run.PiecesSpawned); // the locked-out piece itself still counts.
        Assert.Equal(session.Id, run.RunId);
    }

    // SC-011/DR-104: a lock that both completes a line and locks out counts the clear in the
    // final score and lines-cleared shown on the CompletedRun (EC-4).
    [Fact]
    public void LockOut_ThatAlsoClearsALine_IncludesTheClearInTheFinalSummary()
    {
        var session = new GameSession(pieceQueueSeed: 23);
        session.Start(DateTimeOffset.UtcNow);

        // A T piece locking entirely in buffer rows (per the existing SC-002/SC-024 scenario),
        // with row 19 pre-filled around it so the same lock also completes that row.
        var piece = new ActivePiece
        {
            Type = TetrominoType.T,
            Orientation = Orientation.Spawn,
            Position = new CellPosition(3, 18)
        };
        session.ActivePiece = piece;
        var occupiedByPiece = piece.OccupiedCells.Select(c => c.Column).ToHashSet();

        for (var column = 0; column < Playfield.ColumnCount; column++)
        {
            if (!occupiedByPiece.Contains(column))
            {
                session.Playfield.GetCell(19, column).Occupant = TetrominoType.O;
            }
        }
        foreach (var cell in piece.OccupiedCells)
        {
            if (cell.Row != 19)
            {
                session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
            }
        }
        Assert.True(piece.IsGrounded(session.Playfield));

        var linesBefore = session.LinesCleared;
        TickOverMultipleCalls(session, TimeSpan.FromMilliseconds(500));

        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.NotNull(session.CompletedRun);
        Assert.Equal(TopOutReason.LockOut, session.CompletedRun!.TopOutReason);
        Assert.Equal(linesBefore + 1, session.LinesCleared); // the terminal clear resolved (DR-85 step 4).
        Assert.Equal(session.Score, session.CompletedRun!.FinalScore); // and is reflected in the summary (DR-104).
        Assert.Equal(session.LinesCleared, session.CompletedRun!.LinesCleared);
    }

    // DR-104/SC-010: every figure on the CompletedRun is a frozen snapshot -- further ticks after
    // game over (which are no-ops anyway, per DR-101/NFR-002) never change it.
    [Fact]
    public void CompletedRun_FiguresStayFrozen_AfterGameOver()
    {
        var session = new GameSession(pieceQueueSeed: 19);
        session.Start(DateTimeOffset.UtcNow);
        LockActivePieceOnFloor(session);

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
        LockActivePieceOnFloor(session);

        Assert.Equal(GameStatus.GameOver, session.Status);
        var runBefore = session.CompletedRun!;
        var scoreBefore = runBefore.FinalScore;
        var linesBefore = runBefore.LinesCleared;
        var levelBefore = runBefore.FinalLevel;
        var piecesBefore = runBefore.PiecesSpawned;

        session.Tick(TimeSpan.FromSeconds(10));
        session.PressLeft();
        session.RotateClockwise();
        session.HardDrop();
        session.Hold();

        Assert.Same(runBefore, session.CompletedRun); // exactly one CompletedRun ever (DR-103).
        Assert.Equal(scoreBefore, session.CompletedRun!.FinalScore);
        Assert.Equal(linesBefore, session.CompletedRun!.LinesCleared);
        Assert.Equal(levelBefore, session.CompletedRun!.FinalLevel);
        Assert.Equal(piecesBefore, session.CompletedRun!.PiecesSpawned);
    }

    // DR-103/SC-017: a session discarded mid-play (never topped out) produces no CompletedRun.
    [Fact]
    public void PlayingSession_ThatNeverTopsOut_HasNoCompletedRunAndNoEndedAt()
    {
        var session = new GameSession(pieceQueueSeed: 1);
        session.Start(DateTimeOffset.UtcNow);

        session.Tick(TimeSpan.FromMilliseconds(100));
        session.PressLeft();

        Assert.Equal(GameStatus.Playing, session.Status);
        Assert.Null(session.CompletedRun);
        Assert.Null(session.EndedAt);
    }

    // DR-105/SC-012: a fresh session (play again / new game) starts with no CompletedRun of its
    // own and carries nothing over from a previous, finished session.
    [Fact]
    public void FreshSession_AfterPreviousGameOver_StartsWithNoCompletedRun()
    {
        var finished = new GameSession(pieceQueueSeed: 19);
        finished.Start(DateTimeOffset.UtcNow);
        LockActivePieceOnFloor(finished);
        var nextType = finished.PieceQueue.Upcoming[0];
        var nextDefinition = TetrominoDefinition.For(nextType);
        var minColumnOffset = nextDefinition.SpawnOffsets.Min(o => o.ColumnOffset);
        var maxRowOffset = nextDefinition.SpawnOffsets.Max(o => o.RowOffset);
        var spawnColumn = (Playfield.ColumnCount - nextDefinition.SpawnWidth) / 2 - minColumnOffset;
        var spawnRow = 19 - maxRowOffset;
        foreach (var offset in nextDefinition.SpawnOffsets)
        {
            finished.Playfield.GetCell(spawnRow + offset.RowOffset, spawnColumn + offset.ColumnOffset).Occupant = TetrominoType.O;
        }
        LockActivePieceOnFloor(finished);
        Assert.Equal(GameStatus.GameOver, finished.Status);
        Assert.NotNull(finished.CompletedRun);

        // SC-018: a brand-new GameSession is a distinct aggregate with its own id (the "play
        // again" action, per the UI, always constructs a new GameSession instance -- DR-105).
        var fresh = new GameSession(pieceQueueSeed: 1);
        fresh.Start(DateTimeOffset.UtcNow);

        Assert.NotEqual(finished.Id, fresh.Id);
        Assert.Null(fresh.CompletedRun);
        Assert.Null(fresh.EndedAt);
        Assert.Equal(0, fresh.Score);
        Assert.Equal(0, fresh.LinesCleared);
        Assert.Equal(1, fresh.Level);
        Assert.Equal(1, fresh.PiecesSpawned);
        Assert.Null(fresh.HeldType);
    }

    // DR-107/SC-018: distinct GameSessions (as "play again" always creates) never collide on id.
    [Fact]
    public void EachGameSession_HasADistinctId()
    {
        var a = new GameSession(pieceQueueSeed: 1);
        var b = new GameSession(pieceQueueSeed: 2);
        var c = new GameSession(pieceQueueSeed: 3);

        Assert.NotEqual(a.Id, b.Id);
        Assert.NotEqual(b.Id, c.Id);
        Assert.NotEqual(a.Id, c.Id);
    }

    // DR-101/NFR-002/SC-006: gameplay input at GameOver changes nothing -- extends the existing
    // story 001/004/005 coverage to explicitly assert the CompletedRun and EndedAt are untouched.
    [Fact]
    public void GameOver_IgnoresGameplayInput_CompletedRunAndEndedAtUnchanged()
    {
        var session = new GameSession(pieceQueueSeed: 19);
        session.Start(DateTimeOffset.UtcNow);
        LockActivePieceOnFloor(session);
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
        LockActivePieceOnFloor(session);
        Assert.Equal(GameStatus.GameOver, session.Status);

        var endedAtBefore = session.EndedAt;
        var runBefore = session.CompletedRun;

        for (var i = 0; i < 50; i++)
        {
            session.PressLeft();
            session.PressRight();
            session.RotateClockwise();
            session.RotateCounterClockwise();
            session.PressSoftDrop();
            session.ReleaseSoftDrop();
            session.HardDrop();
            session.Hold();
            session.Tick(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.Equal(endedAtBefore, session.EndedAt);
        Assert.Same(runBefore, session.CompletedRun);
        Assert.Null(session.ActivePiece);
    }

    // DR-103: EndedAt is set exactly once, at the transition, and is never before StartedAt.
    [Fact]
    public void EndedAt_IsSetOnceAtGameOver_AndIsNotBeforeStartedAt()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var session = new GameSession(pieceQueueSeed: 19);
        session.Start(startedAt);

        Assert.Null(session.EndedAt);

        LockActivePieceOnFloor(session);
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
        LockActivePieceOnFloor(session);

        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.NotNull(session.EndedAt);
        Assert.True(session.EndedAt >= startedAt);
        Assert.Equal(session.EndedAt, session.CompletedRun!.EndedAt);
        Assert.Equal(startedAt, session.CompletedRun!.StartedAt);
        Assert.True(session.CompletedRun!.Duration >= TimeSpan.Zero);
    }

    // EC-7/SC-015: a zero-score run (block out before any clear) still produces a perfectly
    // ordinary CompletedRun -- nothing suppresses it.
    [Fact]
    public void EarlyBlockOut_StillProducesACompletedRun_WithZeroScore()
    {
        var session = new GameSession(pieceQueueSeed: 19);
        session.Start(DateTimeOffset.UtcNow);

        var activeType = session.ActivePiece!.Type;
        var activeDefinition = TetrominoDefinition.For(activeType);
        var minColumnOffset = activeDefinition.SpawnOffsets.Min(o => o.ColumnOffset);
        var maxRowOffset = activeDefinition.SpawnOffsets.Max(o => o.RowOffset);
        var spawnColumn = (Playfield.ColumnCount - activeDefinition.SpawnWidth) / 2 - minColumnOffset;
        var spawnRow = 19 - maxRowOffset;

        // Discard the first active piece without locking it, and occupy the very next spawn's
        // cells so the next Spawn() call (triggered manually here) blocks out immediately.
        session.ActivePiece = null;
        foreach (var offset in activeDefinition.SpawnOffsets)
        {
            session.Playfield.GetCell(spawnRow + offset.RowOffset, spawnColumn + offset.ColumnOffset).Occupant = TetrominoType.O;
        }

        session.Spawn();

        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.NotNull(session.CompletedRun);
        Assert.Equal(0, session.CompletedRun!.FinalScore);
        Assert.Equal(0, session.CompletedRun!.LinesCleared);
        Assert.Equal(1, session.CompletedRun!.FinalLevel);
        Assert.Equal(TopOutReason.BlockOut, session.CompletedRun!.TopOutReason);
    }
}
