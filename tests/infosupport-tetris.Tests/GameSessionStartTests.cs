using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for US-001 (docs/specs/001-start-new-game.md), covering SC-001, SC-002, SC-003 and
/// SC-005. SC-004 ("a locked piece keeps its colour") is intentionally NOT covered here: it
/// requires locking a piece into the stack (TetrominoDefinition.colour / DR-24, DR-19), which
/// depends on gravity and locking from stories 003/004 — out of scope for US-001. Once a piece
/// can be locked, a test asserting Cell.Occupant / colour mapping should be added alongside that
/// story's work.
/// </summary>
public class GameSessionStartTests
{
    // SC-001: Starting a game needs one action and no details (FR-001).
    [Fact]
    public void Start_TransitionsReadySessionToPlaying_WithEmptyVisiblePlayfield()
    {
        var session = new GameSession();
        Assert.Equal(GameStatus.Ready, session.Status);

        session.Start(DateTimeOffset.UtcNow);

        Assert.Equal(GameStatus.Playing, session.Status);

        for (var row = 0; row < Playfield.VisibleRowCount; row++)
        {
            for (var column = 0; column < Playfield.ColumnCount; column++)
            {
                Assert.False(session.Playfield.GetVisibleCell(row, column).IsOccupied);
            }
        }
    }

    // SC-002: A new game starts from a clean slate (FR-002).
    [Fact]
    public void NewSession_HasCleanInitialState()
    {
        var session = new GameSession();

        // Every cell, visible and buffer, is empty.
        for (var row = 0; row < Playfield.TotalRowCount; row++)
        {
            for (var column = 0; column < Playfield.ColumnCount; column++)
            {
                Assert.False(session.Playfield.GetCell(row, column).IsOccupied);
            }
        }

        Assert.Equal(1, session.Level);
        Assert.Equal(0, session.Score);
        Assert.True(session.PieceQueue.Upcoming.Count >= 7);
        Assert.Null(session.ActivePiece);
        Assert.Equal(GameStatus.Ready, session.Status);
    }

    // SC-003: Only the visible field is drawn (FR-003, FR-004).
    [Fact]
    public void Playfield_ExposesExactlyTwentyVisibleRowsByTenColumns()
    {
        var session = new GameSession();

        Assert.Equal(20, Playfield.VisibleRowCount);
        Assert.Equal(10, Playfield.ColumnCount);
        Assert.Equal(40, Playfield.TotalRowCount);

        // A buffer-row-only "active piece" occupies rows 0-19 only, so no visible cell should
        // ever reflect buffer-row content — GetVisibleCell only ever reads rows 20-39.
        session.Playfield.GetCell(0, 0).Occupant = TetrominoType.T; // a buffer row
        Assert.False(session.Playfield.GetVisibleCell(0, 0).IsOccupied);
    }

    // SC-005: Restarting discards the current game (FR-005).
    [Fact]
    public void NewSession_AfterDiscardingAPartiallyBuiltOne_StartsFreshAtLevelOneScoreZero()
    {
        var inProgress = new GameSession();
        inProgress.Start(DateTimeOffset.UtcNow);
        // Simulate a partially built stack from a discarded session.
        inProgress.Playfield.GetCell(39, 0).Occupant = TetrominoType.L;

        // FR-005 / DR-23: "New game" discards the current session wholesale and constructs a
        // fresh one — there is no transition method on GameSession for this, a new instance
        // replaces it entirely (see IGameSessionStore.Replace).
        var freshSession = new GameSession();
        freshSession.Start(DateTimeOffset.UtcNow);

        Assert.Equal(GameStatus.Playing, freshSession.Status);
        Assert.Equal(1, freshSession.Level);
        Assert.Equal(0, freshSession.Score);
        for (var row = 0; row < Playfield.TotalRowCount; row++)
        {
            for (var column = 0; column < Playfield.ColumnCount; column++)
            {
                Assert.False(freshSession.Playfield.GetCell(row, column).IsOccupied);
            }
        }

        // The discarded session's data is simply not referenced anywhere by the fresh one.
        Assert.NotEqual(inProgress.Id, freshSession.Id);
    }
}
