using System.Linq;

namespace infosupport_tetris.Domain;

/// <summary>
/// One playthrough, from the start action to game over. The aggregate root for the Play Tetris
/// domain model: everything else (Playfield, ActivePiece, PieceQueue) is reached through it.
/// See spec §5.1. Only FR-001/FR-002/FR-005 (US-001) are implemented here — spawning, gravity,
/// locking and game-over detection belong to later stories and are left as no-ops/stubs.
/// </summary>
public sealed class GameSession
{
    public Guid Id { get; } = Guid.NewGuid();

    public GameStatus Status { get; private set; } = GameStatus.Ready;

    public Playfield Playfield { get; } = new();

    /// <summary>
    /// Null before the first spawn, between lock and spawn, and after game over. US-001 never
    /// spawns a piece, so this stays null for the whole of this spec's scope (spawning is
    /// story 002's responsibility).
    /// </summary>
    public ActivePiece? ActivePiece { get; set; }

    public PieceQueue PieceQueue { get; }

    public int Level { get; private set; } = 1;

    public int Score { get; private set; }

    public int PiecesSpawned { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public GameSession(int? pieceQueueSeed = null)
    {
        PieceQueue = new PieceQueue(pieceQueueSeed);
    }

    /// <summary>
    /// FR-001/FR-002: Ready -> Playing, with a clean Playfield, level 1, score 0, no active
    /// piece, and a freshly seeded piece queue (DR-23). No data entry is required or possible.
    /// </summary>
    public void Start(DateTimeOffset now)
    {
        if (Status != GameStatus.Ready)
        {
            return;
        }

        Status = GameStatus.Playing;
        StartedAt = now;

        // US-002 (FR-010/011): there is no gravity loop yet (story 003), so with nothing else to
        // drive it, the first piece spawns here, immediately on Start. Story 003/004 should call
        // Spawn() again after a lock resolves (DR-20) instead of relying on this call site alone.
        Spawn();
    }

    /// <summary>
    /// Draws the next type from <see cref="PieceQueue"/> and places it per DR-06 (spawn
    /// placement), DR-07 (always Orientation.Spawn) and DR-08 (one free initial descent).
    /// DR-25: only legal when there is no active piece.
    /// <para>
    /// Deliberately NOT implemented here (owned by stories 003/004): gravity ticking, lock delay,
    /// DR-21 block-out / DR-22 lock-out detection and the GameOver transition. If the spawn cells
    /// are already occupied, this method simply does not place a piece and leaves
    /// <see cref="ActivePiece"/> null — story 004 (FR-021/SC-023) must detect that case and move
    /// the session to GameStatus.GameOver. Story 003/004 should also call this method again
    /// immediately after each lock resolves (DR-20), since nothing else currently calls it beyond
    /// the very first spawn in <see cref="Start"/>.
    /// </para>
    /// </summary>
    public void Spawn()
    {
        if (ActivePiece is not null)
        {
            return;
        }

        var type = PieceQueue.Draw();
        var definition = TetrominoDefinition.For(type);

        var minColumnOffset = definition.SpawnOffsets.Min(o => o.ColumnOffset);
        var maxRowOffset = definition.SpawnOffsets.Max(o => o.RowOffset);

        // DR-06: leftmost occupied column at floor((10 - spawnWidth) / 2); lowest occupied cell
        // in row 19 (bottom-most buffer row).
        var spawnColumn = (Playfield.ColumnCount - definition.SpawnWidth) / 2 - minColumnOffset;
        var spawnRow = 19 - maxRowOffset;
        var position = new CellPosition(spawnColumn, spawnRow);

        var piece = new ActivePiece
        {
            Type = type,
            Orientation = Orientation.Spawn, // DR-07
            Position = position
        };

        if (!Playfield.IsValidPlacement(piece.OccupiedCells))
        {
            // DR-21 block out: spawn cells already occupied. Not placed; story 004 detects this
            // (ActivePiece stays null) and transitions the session to GameOver.
            PiecesSpawned++;
            return;
        }

        // DR-08: attempt exactly one free initial descent before any gravity interval elapses.
        var steppedPosition = new CellPosition(position.Column, position.Row + 1);
        var steppedCells = definition.SpawnOffsets
            .Select(o => new CellPosition(steppedPosition.Column + o.ColumnOffset, steppedPosition.Row + o.RowOffset));

        if (Playfield.IsValidPlacement(steppedCells))
        {
            piece.Position = steppedPosition;
        }

        ActivePiece = piece;
        PiecesSpawned++;
    }
}
