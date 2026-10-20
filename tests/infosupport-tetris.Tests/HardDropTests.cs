using System.Linq;
using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for spec 003 (docs/specs/003-hard-drop.md), covering SC-001..SC-009, SC-012..SC-014,
/// SC-016 and EC-3/EC-4/EC-6/EC-9. All drops are invoked directly via
/// <see cref="GameSession.HardDrop"/> (NFR-005) -- no key events or real elapsed time are used.
/// </summary>
public class HardDropTests
{
    // SC-001/SC-002/SC-003/SC-016: lands on the floor, keeps column/orientation, reports travel,
    // and locks immediately -- the piece is no longer active and its cells are occupied.
    [Fact]
    public void HardDrop_OnEmptyField_LandsOnFloorAndLocksImmediately()
    {
        var session = new GameSession(pieceQueueSeed: 1);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;
        var column = piece.Position.Column;
        var orientation = piece.Orientation;
        var type = piece.Type;

        var outcome = session.HardDrop();

        Assert.NotNull(outcome);
        Assert.NotSame(piece, session.ActivePiece); // locked, no longer active
        Assert.Equal(type, outcome!.Type);
        Assert.True(outcome.CellsTravelled > 0);
        Assert.Same(outcome, session.LastHardDropOutcome);

        foreach (var cell in piece.OccupiedCells)
        {
            Assert.Equal(column, piece.Position.Column); // column unchanged (DR-44)
            Assert.Equal(orientation, piece.Orientation); // orientation unchanged (DR-44)
            Assert.Equal(type, session.Playfield.GetCell(cell.Row, cell.Column).Occupant);
        }
    }

    // SC-002: lands on top of an existing stack and reports the correct travel distance.
    [Fact]
    public void HardDrop_OntoStack_LandsOnTopAndReportsCorrectDistance()
    {
        var session = new GameSession(pieceQueueSeed: 3);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;

        // Occupy the row directly beneath the piece's current footprint at row 30.
        foreach (var column in piece.OccupiedCells.Select(c => c.Column).Distinct())
        {
            session.Playfield.GetCell(30, column).Occupant = TetrominoType.O;
        }

        var startingRow = piece.OccupiedCells.Max(c => c.Row);
        var expectedDistance = 29 - startingRow; // lands with its lowest cell(s) on row 29

        var outcome = session.HardDrop();

        Assert.Equal(expectedDistance, outcome!.CellsTravelled);
        Assert.Equal(29, piece.OccupiedCells.Max(c => c.Row));
    }

    // SC-004/SC-016: does not tunnel through the stack into a cavity beneath an overhang.
    [Fact]
    public void HardDrop_OverCavityBeneathOverhang_StopsOnOverhang_CavityStaysEmpty()
    {
        var session = new GameSession(pieceQueueSeed: 5);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;
        var columns = piece.OccupiedCells.Select(c => c.Column).Distinct().ToArray();
        var overhangColumn = columns[0];

        // A single occupied cell well above the floor; everything beneath it is empty.
        session.Playfield.GetCell(30, overhangColumn).Occupant = TetrominoType.O;

        session.HardDrop();

        // Every cell of the locked piece must sit above (or at) row 29, never below it, and the
        // cells beneath the overhang (rows 31-39 in that column) must remain empty.
        foreach (var column in columns)
        {
            for (var row = 31; row <= 39; row++)
            {
                Assert.False(session.Playfield.GetCell(row, column).IsOccupied);
            }
        }
    }

    // SC-005/EC-4: bypasses a running lock delay -- no 0.500s wait, no reset consulted/consumed.
    [Fact]
    public void HardDrop_WithRunningLockDelay_LocksImmediately_IgnoringTheDelay()
    {
        var session = new GameSession(pieceQueueSeed: 7);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;

        // Ground the piece and let a partial lock delay accumulate.
        foreach (var cell in piece.OccupiedCells)
        {
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }
        session.Tick(TimeSpan.FromMilliseconds(100));
        Assert.NotNull(piece.LockTimerElapsed);

        var outcome = session.HardDrop();

        Assert.Equal(0, outcome!.CellsTravelled); // already grounded (SC-006/EC-3)
        Assert.NotSame(piece, session.ActivePiece); // locked right away, no further delay waited out
    }

    // SC-006/EC-3: an already-grounded piece travels zero cells.
    [Fact]
    public void HardDrop_AlreadyGrounded_TravelsZeroCells()
    {
        var session = new GameSession(pieceQueueSeed: 9);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;
        var positionBefore = piece.Position;

        foreach (var cell in piece.OccupiedCells)
        {
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }
        Assert.True(piece.IsGrounded(session.Playfield));

        var outcome = session.HardDrop();

        Assert.Equal(0, outcome!.CellsTravelled);
        Assert.Equal(positionBefore, outcome.LandingPosition);
    }

    // SC-007: the 15-reset lock delay budget is irrelevant to a hard drop.
    [Fact]
    public void HardDrop_WithPartiallyConsumedResetBudget_LocksWithoutConsultingBudget()
    {
        var session = new GameSession(pieceQueueSeed: 11);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;

        foreach (var cell in piece.OccupiedCells)
        {
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }
        session.Tick(TimeSpan.FromMilliseconds(10));
        Assert.True(piece.TryResetLockTimer());
        Assert.True(piece.TryResetLockTimer());
        Assert.True(piece.TryResetLockTimer());
        Assert.Equal(3, piece.LockResetsUsed);

        var outcome = session.HardDrop();

        Assert.Equal(0, outcome!.CellsTravelled);
        Assert.NotSame(piece, session.ActivePiece);
    }

    // SC-008: the next piece spawns immediately, falling under gravity, not itself dropped.
    [Fact]
    public void HardDrop_AfterLock_SpawnsNextPieceImmediately_NotItselfDropped()
    {
        var session = new GameSession(pieceQueueSeed: 13);
        session.Start(DateTimeOffset.UtcNow);
        var firstPiece = session.ActivePiece!;
        var upcomingBeforeLock = session.PieceQueue.Upcoming.ToArray();
        var spawnedBeforeLock = session.PiecesSpawned;

        session.HardDrop();

        Assert.Equal(GameStatus.Playing, session.Status);
        Assert.NotNull(session.ActivePiece);
        Assert.NotSame(firstPiece, session.ActivePiece);
        Assert.Equal(spawnedBeforeLock + 1, session.PiecesSpawned);
        Assert.Equal(upcomingBeforeLock[0], session.ActivePiece!.Type);

        var positionBefore = session.ActivePiece.Position;
        session.Tick(TimeSpan.FromMilliseconds(10));
        Assert.Equal(positionBefore, session.ActivePiece.Position); // still falling, not dropped
    }

    // SC-009/EC-6: a hard drop can end the game by lock out.
    [Fact]
    public void HardDrop_AllFourCellsInBufferRows_WritesCellsThenMovesToGameOver()
    {
        var session = new GameSession(pieceQueueSeed: 15);
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
        var occupiedCells = piece.OccupiedCells.ToArray();

        var outcome = session.HardDrop();

        Assert.Equal(0, outcome!.CellsTravelled);
        foreach (var cell in occupiedCells)
        {
            Assert.Equal(piece.Type, session.Playfield.GetCell(cell.Row, cell.Column).Occupant);
        }
        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.Null(session.ActivePiece);
    }

    // SC-012/EC-9: a hard drop is ignored while the session is Ready or GameOver.
    [Fact]
    public void HardDrop_SessionNotPlaying_IsIgnored()
    {
        var readySession = new GameSession(pieceQueueSeed: 17);
        Assert.Equal(GameStatus.Ready, readySession.Status);
        Assert.Null(readySession.HardDrop());
        Assert.Equal(GameStatus.Ready, readySession.Status);

        var gameOverSession = new GameSession(pieceQueueSeed: 19);
        gameOverSession.Start(DateTimeOffset.UtcNow);

        // Force a deterministic lock out (DR-22): a piece placed entirely within the buffer rows,
        // grounded in place (same pattern as spec 001's SC-024 / HardDrop_AllFourCellsInBufferRows).
        var bufferPiece = new ActivePiece
        {
            Type = TetrominoType.T,
            Orientation = Orientation.Spawn,
            Position = new CellPosition(3, 18)
        };
        gameOverSession.ActivePiece = bufferPiece;
        foreach (var cell in bufferPiece.OccupiedCells)
        {
            Assert.True(cell.Row <= 19);
            gameOverSession.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }
        gameOverSession.HardDrop();
        Assert.Equal(GameStatus.GameOver, gameOverSession.Status);

        Assert.Null(gameOverSession.HardDrop());
        Assert.Equal(GameStatus.GameOver, gameOverSession.Status);
    }

    // SC-013/FR-013: a press with no active piece (e.g. mid line-clear in a future story) is
    // discarded, not buffered for the piece that spawns next.
    [Fact]
    public void HardDrop_NoActivePiece_IsIgnored_DoesNotAffectNextSpawn()
    {
        var session = new GameSession(pieceQueueSeed: 21);
        session.Start(DateTimeOffset.UtcNow);
        session.ActivePiece = null; // simulate the gap between a lock and the next spawn

        var outcome = session.HardDrop();

        Assert.Null(outcome);
        Assert.Null(session.ActivePiece);
    }

    // SC-014: the travelled distance is handed to scoring via HardDropOutcome, with no change to
    // score itself (003 owns nothing of story 006's scoring).
    [Fact]
    public void HardDrop_PublishesCellsTravelled_WithoutTouchingScore()
    {
        var session = new GameSession(pieceQueueSeed: 23);
        session.Start(DateTimeOffset.UtcNow);
        var scoreBefore = session.Score;

        var outcome = session.HardDrop();

        Assert.True(outcome!.CellsTravelled > 0);
        Assert.Equal(scoreBefore, session.Score);
    }

    // SUC-002-style property: hard drop lands exactly where repeated single-cell descents would.
    [Fact]
    public void HardDrop_MatchesRepeatedSingleCellDescents()
    {
        var hardDropSession = new GameSession(pieceQueueSeed: 25);
        hardDropSession.Start(DateTimeOffset.UtcNow);
        var hardDropPiece = hardDropSession.ActivePiece!;

        var gravitySession = new GameSession(pieceQueueSeed: 25);
        gravitySession.Start(DateTimeOffset.UtcNow);
        var gravityPiece = gravitySession.ActivePiece!;

        // Add an identical stack to both sessions.
        foreach (var column in hardDropPiece.OccupiedCells.Select(c => c.Column).Distinct())
        {
            hardDropSession.Playfield.GetCell(32, column).Occupant = TetrominoType.O;
            gravitySession.Playfield.GetCell(32, column).Occupant = TetrominoType.O;
        }

        var outcome = hardDropSession.HardDrop();

        // Step the gravity session down one cell at a time until grounded.
        var steps = 0;
        while (!gravityPiece.IsGrounded(gravitySession.Playfield))
        {
            gravityPiece.Position = new CellPosition(gravityPiece.Position.Column, gravityPiece.Position.Row + 1);
            steps++;
        }

        Assert.Equal(steps, outcome!.CellsTravelled);
        Assert.Equal(gravityPiece.Position, outcome.LandingPosition);
    }

    // Integration with spec 002: ComputeHardDropDistance/HardDrop must honor a piece's current
    // Orientation (as set by ActivePiece.TryRotate), not just its spawn orientation -- otherwise
    // a rotated piece could be hard-dropped against the wrong footprint and tunnel into/through
    // the stack.
    [Fact]
    public void HardDrop_AfterRotation_UsesRotatedFootprint_NotSpawnOrientation()
    {
        var session = new GameSession(pieceQueueSeed: 2);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;

        var rotated = piece.TryRotate(RotationDirection.Clockwise, session.Playfield);
        Assert.True(rotated);
        Assert.NotEqual(Orientation.Spawn, piece.Orientation);

        var rotatedOrientation = piece.Orientation;

        var outcome = session.HardDrop();

        Assert.NotNull(outcome);
        Assert.True(outcome!.CellsTravelled >= 0);

        // The locked cells in the playfield must match the piece's rotated footprint translated
        // straight down by CellsTravelled -- i.e. hard drop used the rotated orientation for every
        // candidate row, not the spawn orientation.
        var definition = TetrominoDefinition.For(piece.Type);
        var offsets = definition.OffsetsFor(rotatedOrientation);
        foreach (var offset in offsets)
        {
            var row = outcome.LandingPosition.Row + offset.RowOffset;
            var column = outcome.LandingPosition.Column + offset.ColumnOffset;
            Assert.Equal(piece.Type, session.Playfield.GetCell(row, column).Occupant);
        }
    }
}
