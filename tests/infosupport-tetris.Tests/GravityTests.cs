using System.Linq;
using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for US-003 (docs/specs/001-start-new-game.md), covering SC-015, SC-016, SC-025, and
/// edge cases EC-2 (100ms-per-call cap) and EC-10 (zero/negative delta is a no-op). All timing
/// is driven through <see cref="GameSession.Tick"/> with simulated deltas (NFR-006) — no real
/// elapsed wall-clock time is used anywhere in this file.
/// </summary>
public class GravityTests
{
    // SC-015: Gravity moves a piece one cell per second at level 1 (FR-013, FR-014).
    [Fact]
    public void Tick_OneSecond_MovesPieceDownExactlyOneCell()
    {
        var session = new GameSession(pieceQueueSeed: 1);
        session.Start(DateTimeOffset.UtcNow);
        var startRow = session.ActivePiece!.Position.Row;

        // A single Tick call consumes at most 100ms (DR-26/EC-2) even when the delta itself is
        // smaller than that cap would otherwise allow for in one go elsewhere; simulate
        // 1.000s of game time as ten 100ms calls.
        for (var i = 0; i < 10; i++)
        {
            session.Tick(TimeSpan.FromMilliseconds(100));
        }

        Assert.Equal(startRow + 1, session.ActivePiece!.Position.Row);

        // "a further 3.000 seconds elapse" -> three more cells. A single Tick call is capped to
        // 100ms (DR-26/EC-2), so simulate 3.000s as thirty 100ms calls.
        for (var i = 0; i < 30; i++)
        {
            session.Tick(TimeSpan.FromMilliseconds(100));
        }

        Assert.Equal(startRow + 4, session.ActivePiece!.Position.Row);
    }

    // SC-016: Gravity is unaffected by frame rate (FR-014).
    [Fact]
    public void Tick_SameTotalElapsedTime_ProducesSameFallDistance_RegardlessOfStepSize()
    {
        var sixtyFps = new GameSession(pieceQueueSeed: 7);
        sixtyFps.Start(DateTimeOffset.UtcNow);
        var sixtyFpsStartRow = sixtyFps.ActivePiece!.Position.Row;

        var thirtyFps = new GameSession(pieceQueueSeed: 7);
        thirtyFps.Start(DateTimeOffset.UtcNow);
        var thirtyFpsStartRow = thirtyFps.ActivePiece!.Position.Row;

        Assert.Equal(sixtyFpsStartRow, thirtyFpsStartRow);

        // 10 seconds of wall-clock time, delivered as 1/60s steps vs 1/30s steps. Both step
        // sizes are well under the 100ms-per-call cap, so neither is affected by it.
        var sixtyFpsStep = TimeSpan.FromSeconds(1.0 / 60);
        for (var i = 0; i < 600; i++)
        {
            sixtyFps.Tick(sixtyFpsStep);
        }

        var thirtyFpsStep = TimeSpan.FromSeconds(1.0 / 30);
        for (var i = 0; i < 300; i++)
        {
            thirtyFps.Tick(thirtyFpsStep);
        }

        Assert.Equal(sixtyFps.ActivePiece!.Position.Row, thirtyFps.ActivePiece!.Position.Row);
        // Sanity: roughly 10 seconds at 1.000s/cell should move the piece down about 10 cells
        // from its post-spawn position (allowing a cell either way for floating-point rounding
        // in accumulating many small TimeSpan deltas — the point of this scenario is that both
        // frame rates agree, not the exact count).
        Assert.InRange(sixtyFps.ActivePiece!.Position.Row, sixtyFpsStartRow + 9, sixtyFpsStartRow + 10);
    }

    // SC-025: Only one piece falls at a time (FR-023).
    [Fact]
    public void GameSession_NeverHoldsMoreThanOneActivePiece()
    {
        var session = new GameSession(pieceQueueSeed: 3);
        session.Start(DateTimeOffset.UtcNow);

        Assert.NotNull(session.ActivePiece);
        var pieceBefore = session.ActivePiece;

        // DR-25: a spawn is only legal when there is no active piece. Calling Spawn() again
        // while one is active must be a no-op, not a second piece.
        session.Spawn();

        Assert.Same(pieceBefore, session.ActivePiece);

        // Advancing gravity never creates a second piece either.
        session.Tick(TimeSpan.FromSeconds(1.0));
        Assert.Same(pieceBefore, session.ActivePiece);
    }

    // EC-2: a huge time delta (e.g. a backgrounded tab) is capped to 100ms per Tick call.
    [Fact]
    public void Tick_HugeElapsedDelta_ConsumesAtMostOneHundredMilliseconds_NoCatchUpBurst()
    {
        var session = new GameSession(pieceQueueSeed: 2);
        session.Start(DateTimeOffset.UtcNow);
        var startRow = session.ActivePiece!.Position.Row;

        // A single call with a 10-second delta must only ever consume 100ms of it (DR-26/EC-2),
        // which at the 1.000s gravityInterval is nowhere near enough to move the piece at all —
        // i.e. no multi-cell catch-up burst.
        session.Tick(TimeSpan.FromSeconds(10));

        Assert.Equal(startRow, session.ActivePiece!.Position.Row);

        // Confirm the capped 100ms really was accumulated (not silently dropped) by feeding the
        // other 900ms across nine more calls to reach exactly one gravityInterval.
        for (var i = 0; i < 9; i++)
        {
            session.Tick(TimeSpan.FromMilliseconds(100));
        }

        Assert.Equal(startRow + 1, session.ActivePiece!.Position.Row);
    }

    // EC-10: a zero or negative delta is treated as zero; the piece never moves and never runs
    // the timer backwards.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    public void Tick_ZeroOrNegativeDelta_IsANoOp(int milliseconds)
    {
        var session = new GameSession(pieceQueueSeed: 4);
        session.Start(DateTimeOffset.UtcNow);
        var startRow = session.ActivePiece!.Position.Row;
        var startFallTimer = session.ActivePiece!.FallTimerElapsed;

        session.Tick(TimeSpan.FromMilliseconds(milliseconds));

        Assert.Equal(startRow, session.ActivePiece!.Position.Row);
        Assert.Equal(startFallTimer, session.ActivePiece!.FallTimerElapsed);
    }

    // DR-15: groundedness is recomputed from the current piece/stack, never cached — used here
    // as the extension point US-004 will build lock delay on top of.
    [Fact]
    public void IsGrounded_TrueOnlyWhenOneCellDownWouldBeInvalid()
    {
        var session = new GameSession(pieceQueueSeed: 5);
        session.Start(DateTimeOffset.UtcNow);
        var piece = session.ActivePiece!;

        Assert.False(piece.IsGrounded(session.Playfield));

        // Occupy the row directly beneath every cell the piece currently occupies.
        foreach (var cell in piece.OccupiedCells)
        {
            session.Playfield.GetCell(cell.Row + 1, cell.Column).Occupant = TetrominoType.O;
        }

        Assert.True(piece.IsGrounded(session.Playfield));

        // Gravity must then leave the piece exactly where it is.
        var rowBeforeTick = piece.Position.Row;
        session.Tick(TimeSpan.FromSeconds(5.0));
        Assert.Equal(rowBeforeTick, piece.Position.Row);
    }
}
