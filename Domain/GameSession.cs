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
    }
}
