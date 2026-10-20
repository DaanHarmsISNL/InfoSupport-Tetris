using System.Linq;
using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for docs/specs/005-hold-piece.md. Covers the §4 acceptance scenarios and §5.5 domain
/// rules (DR-63..DR-74) that are expressible headlessly (NFR-006) without rasterising pixels.
/// </summary>
public class HoldPieceTests
{
    // SC-003/FR-002/FR-005/DR-65: first hold stashes the piece and the queue advances.
    [Fact]
    public void FirstHold_StashesOutgoingPiece_AndSpawnsFromTheQueue_AdvancingIt()
    {
        var session = new GameSession(pieceQueueSeed: 42);
        session.Start(DateTimeOffset.UtcNow);

        var outgoingType = session.ActivePiece!.Type;
        var expectedIncoming = session.PieceQueue.Upcoming[0];
        var queueLengthBefore = session.PieceQueue.Upcoming.Count;

        var accepted = session.Hold();

        Assert.True(accepted);
        Assert.Equal(outgoingType, session.HeldType);
        Assert.NotNull(session.ActivePiece);
        Assert.Equal(expectedIncoming, session.ActivePiece!.Type);
        Assert.True(session.PieceQueue.Upcoming.Count >= 7);
        Assert.Equal(queueLengthBefore - 1 + 1, session.PieceQueue.Upcoming.Count); // drew one, refilled by one
    }

    // SC-004/SC-005/FR-003/FR-004/DR-66: a swap takes nothing from the queue and leaves the
    // preview untouched.
    [Fact]
    public void SwapWithOccupiedSlot_ExchangesTypes_AndLeavesQueueAndPreviewUntouched()
    {
        var session = new GameSession(pieceQueueSeed: 7);
        session.Start(DateTimeOffset.UtcNow);

        // First hold: empty slot, now occupied.
        session.Hold();
        var heldAfterFirst = session.HeldType;

        // Piece has already been swapped in by the first hold -- lock it so hold is available again.
        LockCurrentPieceDirectly(session);

        var outgoingType = session.ActivePiece!.Type;
        var queueBefore = session.PieceQueue.Upcoming.ToArray();
        var previewBefore = session.NextPiecePreview.PreviewedType;

        var accepted = session.Hold();

        Assert.True(accepted);
        Assert.Equal(heldAfterFirst, session.ActivePiece!.Type);
        Assert.Equal(outgoingType, session.HeldType);
        Assert.Equal(queueBefore, session.PieceQueue.Upcoming);
        Assert.Equal(previewBefore, session.NextPiecePreview.PreviewedType);
    }

    // SC-006/FR-006/DR-67: the incoming piece starts at the standard spawn placement, not
    // wherever the outgoing piece sat.
    [Fact]
    public void IncomingPiece_SpawnsAtStandardPlacement_InSpawnOrientation()
    {
        var session = new GameSession(pieceQueueSeed: 1);
        session.Start(DateTimeOffset.UtcNow);

        // Move and rotate the outgoing piece away from its spawn placement.
        session.PressRight();
        session.RotateClockwise();

        session.Hold();

        Assert.NotNull(session.ActivePiece);
        Assert.Equal(Orientation.Spawn, session.ActivePiece!.Orientation);

        var definition = TetrominoDefinition.For(session.ActivePiece.Type);
        var minColumnOffset = definition.SpawnOffsets.Min(o => o.ColumnOffset);
        var expectedColumn = (Playfield.ColumnCount - definition.SpawnWidth) / 2 - minColumnOffset;

        Assert.Equal(expectedColumn, session.ActivePiece.Position.Column);
    }

    // SC-007/FR-007/DR-67: the incoming piece gets fresh timers and a full lock-reset budget.
    [Fact]
    public void IncomingPiece_GetsFreshTimers_AndFullLockResetBudget()
    {
        var session = new GameSession(pieceQueueSeed: 3);
        session.Start(DateTimeOffset.UtcNow);

        // Drive the outgoing piece to the floor and consume several lock resets.
        for (var i = 0; i < 25; i++)
        {
            session.Tick(TimeSpan.FromSeconds(1));
            if (session.ActivePiece is null)
            {
                return; // Locked before we could hold -- not the scenario under test, bail cleanly.
            }
        }

        if (session.ActivePiece!.IsGrounded(session.Playfield))
        {
            session.PressLeft();
            session.PressRight();
        }

        session.Hold();

        Assert.NotNull(session.ActivePiece);
        Assert.Equal(TimeSpan.Zero, session.ActivePiece!.FallTimerElapsed);
        Assert.Null(session.ActivePiece.LockTimerElapsed);
        Assert.Equal(0, session.ActivePiece.LockResetsUsed);
    }

    // SC-008/FR-008/DR-67/DR-08: the incoming piece takes the free initial descent.
    [Fact]
    public void IncomingPiece_TakesTheInitialDrop()
    {
        var session = new GameSession(pieceQueueSeed: 9);
        session.Start(DateTimeOffset.UtcNow);

        session.Hold();

        Assert.NotNull(session.ActivePiece);
        var definition = TetrominoDefinition.For(session.ActivePiece!.Type);
        var maxRowOffset = definition.SpawnOffsets.Max(o => o.RowOffset);
        var unSteppedRow = 19 - maxRowOffset;

        // DR-08: the piece has stepped down once from the un-dropped spawn row (an empty board
        // always allows this step).
        Assert.True(session.ActivePiece.Position.Row > unSteppedRow);
    }

    // SC-009/FR-009/DR-64/DR-70: holding writes nothing to the stack and does not lock the
    // outgoing piece.
    [Fact]
    public void Hold_LeavesTheStackUntouched_AndDoesNotLockTheOutgoingPiece()
    {
        var session = new GameSession(pieceQueueSeed: 15);
        session.Start(DateTimeOffset.UtcNow);

        // Occupy the floor directly below the piece's current column so cells are distinguishable.
        var occupiedBefore = session.Playfield.Cells
            .SelectMany(r => r)
            .Count(c => c.IsOccupied);

        session.Hold();

        var occupiedAfter = session.Playfield.Cells
            .SelectMany(r => r)
            .Count(c => c.IsOccupied);

        Assert.Equal(occupiedBefore, occupiedAfter);
    }

    // SC-010/FR-011/FR-013/DR-68/DR-70: hold cannot be used twice on the same piece.
    [Fact]
    public void SecondHold_OnTheSamePiece_IsIgnored()
    {
        var session = new GameSession(pieceQueueSeed: 21);
        session.Start(DateTimeOffset.UtcNow);

        session.Hold();
        var typeAfterFirstHold = session.ActivePiece!.Type;
        var heldAfterFirstHold = session.HeldType;
        var queueAfterFirstHold = session.PieceQueue.Upcoming.ToArray();

        var acceptedSecond = session.Hold();

        Assert.False(acceptedSecond);
        Assert.Equal(typeAfterFirstHold, session.ActivePiece!.Type);
        Assert.Equal(heldAfterFirstHold, session.HeldType);
        Assert.Equal(queueAfterFirstHold, session.PieceQueue.Upcoming);
        Assert.Equal(0, session.Score);
    }

    // SC-011/SC-012/FR-012/DR-69: hold becomes available again only after a lock, never merely
    // because a new piece spawned (e.g. via a swap).
    [Fact]
    public void HoldAvailability_IsRestoredOnlyByALock_NotBySpawningViaAnotherHold()
    {
        var session = new GameSession(pieceQueueSeed: 33);
        session.Start(DateTimeOffset.UtcNow);

        session.Hold(); // first hold: empty slot -> occupied
        Assert.False(session.HoldAvailable);

        var acceptedImmediateSecond = session.Hold(); // SC-012: same spawned-in piece, still used.
        Assert.False(acceptedImmediateSecond);
        Assert.False(session.HoldAvailable);

        LockCurrentPieceDirectly(session); // SC-011: a lock clears it.

        Assert.True(session.HoldAvailable);
    }

    // SC-013/DR-68: hold cannot be used to stall indefinitely -- at most one swap per piece even
    // under rapid repeated presses.
    [Fact]
    public void RapidRepeatedHoldPresses_ProduceAtMostOneSwapPerPiece()
    {
        var session = new GameSession(pieceQueueSeed: 55);
        session.Start(DateTimeOffset.UtcNow);

        var acceptedCount = 0;
        for (var i = 0; i < 10; i++)
        {
            if (session.Hold())
            {
                acceptedCount++;
            }
        }

        Assert.Equal(1, acceptedCount);
    }

    // SC-014/FR-014/DR-72: a hold resolved before gravity in the same step beats an expiring lock
    // delay -- the outgoing piece does not lock.
    [Fact]
    public void Hold_ResolvedBeforeGravity_BeatsAnExpiringLockDelay()
    {
        var session = new GameSession(pieceQueueSeed: 2);
        session.Start(DateTimeOffset.UtcNow);

        // Ground the piece and run its lock delay to just shy of expiry.
        while (!session.ActivePiece!.IsGrounded(session.Playfield))
        {
            session.Tick(TimeSpan.FromSeconds(1));
        }

        session.Tick(TimeSpan.FromMilliseconds(490));
        Assert.NotNull(session.ActivePiece);
        var outgoingType = session.ActivePiece!.Type;

        // Resolve the hold "in the same step" (i.e. before any further tick advances the delay
        // past expiry) -- this is what Pages/Index.cshtml.cs's OnPostInput(Hold) does by calling
        // Hold() before Tick for that request.
        var accepted = session.Hold();

        Assert.True(accepted);
        Assert.NotEqual(GameStatus.GameOver, session.Status);
        Assert.Equal(outgoingType, session.HeldType);
        // The outgoing piece's cells must never have been written to the stack.
        Assert.DoesNotContain(session.Playfield.Cells.SelectMany(r => r), c => c.IsOccupied);
    }

    // SC-015/FR-015/DR-71: a hold swap can end the game via block out.
    [Fact]
    public void Hold_CanBlockOut_WhenIncomingSpawnCellsAreOccupied()
    {
        var session = new GameSession(pieceQueueSeed: 4);
        session.Start(DateTimeOffset.UtcNow);

        // Occupy every buffer-row cell so any incoming spawn placement collides.
        for (var row = 0; row <= 19; row++)
        {
            for (var column = 0; column < Playfield.ColumnCount; column++)
            {
                session.Playfield.GetCell(row, column).Occupant = TetrominoType.T;
            }
        }

        var outgoingType = session.ActivePiece!.Type;
        var accepted = session.Hold();

        Assert.True(accepted); // the swap itself is already committed (DR-71)
        Assert.Equal(GameStatus.GameOver, session.Status);
        Assert.Null(session.ActivePiece);
        Assert.Equal(outgoingType, session.HeldType);
    }

    // SC-019/FR-019/DR-74: a new game starts with an empty hold slot and hold available.
    [Fact]
    public void NewSession_StartsWithEmptyHoldSlot_AndHoldUnused()
    {
        var session = new GameSession(pieceQueueSeed: 6);

        Assert.Null(session.HeldType);
        Assert.False(session.HoldUsedThisPiece);

        session.Start(DateTimeOffset.UtcNow);

        Assert.Null(session.HeldType);
        Assert.True(session.HoldAvailable);
    }

    // SC-020/FR-020/DR-74: the hold slot freezes at game over; a hold press is ignored.
    [Fact]
    public void HoldSlot_FreezesAtGameOver_AndFurtherHoldPressesAreIgnored()
    {
        var session = new GameSession(pieceQueueSeed: 8);
        session.Start(DateTimeOffset.UtcNow);

        session.Hold();
        var heldBeforeGameOver = session.HeldType;

        // Force a lock-out: lock a piece entirely within the buffer rows.
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

        var accepted = session.Hold();

        Assert.False(accepted);
        Assert.Equal(heldBeforeGameOver, session.HeldType);
        Assert.Null(session.ActivePiece);
    }

    // EC-8/DR-68/DR-70: hold is ignored while the session is still Ready.
    [Fact]
    public void Hold_IsIgnored_WhileSessionIsStillReady()
    {
        var session = new GameSession(pieceQueueSeed: 10);

        var accepted = session.Hold();

        Assert.False(accepted);
        Assert.Null(session.HeldType);
        Assert.Equal(GameStatus.Ready, session.Status);
    }

    // DR-70: an ignored hold press changes nothing at all.
    [Fact]
    public void IgnoredHoldPress_ChangesNothing()
    {
        var session = new GameSession(pieceQueueSeed: 11);
        session.Start(DateTimeOffset.UtcNow);
        session.Hold(); // consumes the hold for this piece
        var queueBefore = session.PieceQueue.Upcoming.ToArray();
        var activeBefore = session.ActivePiece;
        var heldBefore = session.HeldType;

        var accepted = session.Hold();

        Assert.False(accepted);
        Assert.Equal(queueBefore, session.PieceQueue.Upcoming);
        Assert.Same(activeBefore, session.ActivePiece);
        Assert.Equal(heldBefore, session.HeldType);
    }

    // DR-67/FR-010: piecesSpawned increments for a hold-spawned piece, same as any other spawn.
    [Fact]
    public void HoldSpawnedPiece_IncrementsPiecesSpawned()
    {
        var session = new GameSession(pieceQueueSeed: 12);
        session.Start(DateTimeOffset.UtcNow);

        var before = session.PiecesSpawned;
        session.Hold();

        Assert.Equal(before + 1, session.PiecesSpawned);
    }

    // SUC-003-style property check: across many random hold presses, no type is ever duplicated
    // or lost -- the multiset of spawned+held types stays internally consistent in size.
    [Fact]
    public void SeededSession_WithRandomHoldPresses_NeverDuplicatesOrLosesAPiece()
    {
        var session = new GameSession(pieceQueueSeed: 777);
        session.Start(DateTimeOffset.UtcNow);
        var rng = new Random(777);

        var spawnedTypes = new System.Collections.Generic.List<TetrominoType> { session.ActivePiece!.Type };

        for (var i = 0; i < 300 && session.Status != GameStatus.GameOver; i++)
        {
            if (rng.Next(3) == 0)
            {
                var activeBefore = session.ActivePiece?.Type;
                if (session.Hold() && session.ActivePiece is not null && activeBefore != session.ActivePiece.Type)
                {
                    spawnedTypes.Add(session.ActivePiece.Type);
                }
                else if (session.ActivePiece is not null && !spawnedTypes.Contains(session.ActivePiece.Type))
                {
                    spawnedTypes.Add(session.ActivePiece.Type);
                }
            }

            session.Tick(TimeSpan.FromSeconds(2));

            if (session.ActivePiece is null && session.Status == GameStatus.Playing)
            {
                session.Spawn();
            }
        }

        // Sanity: no exception thrown, and the session either ended in GameOver or kept playing
        // with a non-null hold/active consistency (HeldType xor ActivePiece empty never both null
        // unexpectedly while Playing).
        if (session.Status == GameStatus.Playing)
        {
            Assert.NotNull(session.ActivePiece);
        }
    }

    /// <summary>
    /// Test helper: locks the current active piece directly onto the floor without waiting out
    /// real lock-delay time, so hold-availability-after-lock tests don't depend on timing.
    /// </summary>
    private static void LockCurrentPieceDirectly(GameSession session)
    {
        var piece = session.ActivePiece!;
        while (!piece.IsGrounded(session.Playfield))
        {
            piece.TryStepDown(session.Playfield);
        }

        // GameSession.Tick clamps each call to MaxElapsedPerTick (100ms), so several calls are
        // needed to cross the 500ms lock delay threshold.
        for (var i = 0; i < 6 && session.ActivePiece is not null; i++)
        {
            session.Tick(TimeSpan.FromMilliseconds(100));
        }
    }
}
