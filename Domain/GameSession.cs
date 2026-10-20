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

    /// <summary>
    /// Not part of the spec's domain model — bookkeeping for the UI wiring (<c>Pages/Index</c>),
    /// which calls <see cref="Tick"/> with the real elapsed wall-clock time between page
    /// requests. Tests should call <see cref="Tick"/> directly with simulated deltas instead of
    /// relying on this.
    /// </summary>
    public DateTimeOffset? LastTickAt { get; set; }

    /// <summary>
    /// DR-14: time per one-cell descent, derived from <see cref="Level"/>. This spec fixes the
    /// game at level 1, so this is always 1.000 second; the Guideline curve that varies it by
    /// level is story 007's concern.
    /// </summary>
    public TimeSpan GravityInterval => TimeSpan.FromSeconds(1.0);

    /// <summary>
    /// DR-26/EC-2: the maximum elapsed time a single <see cref="Tick"/> call consumes, so a
    /// stalled/backgrounded tab cannot produce a multi-cell catch-up burst.
    /// </summary>
    public static readonly TimeSpan MaxElapsedPerTick = TimeSpan.FromMilliseconds(100);

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
        LastTickAt = now;

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

    /// <summary>
    /// US-003 (FR-013/FR-014/FR-023, DR-14, DR-15, DR-26). Advances the active piece's gravity
    /// timer by <paramref name="elapsed"/>, capped per call to <see cref="MaxElapsedPerTick"/>
    /// (DR-26/EC-2), and steps the piece down one cell per whole <see cref="GravityInterval"/>
    /// that has accumulated — but only while the piece is not grounded (DR-15), and only when
    /// the step itself is a valid placement (DR-03/DR-04).
    /// <para>
    /// A zero or negative <paramref name="elapsed"/> (EC-10, e.g. a clock hiccup) is a no-op:
    /// the piece never moves up and the timer never runs backwards.
    /// </para>
    /// <para>
    /// Deliberately NOT implemented here (owned by story 004/US-004): once the piece is
    /// grounded, this method simply stops acting on it — no lock delay timer is started, no
    /// locking happens, and no next piece spawns. <see cref="ActivePiece.IsGrounded"/> is the
    /// extension point US-004 builds its lock delay on top of; <see cref="ActivePiece.LockTimerElapsed"/>,
    /// <see cref="ActivePiece.LockResetsUsed"/> and <see cref="ActivePiece.LowestRowReached"/>
    /// remain untouched placeholders.
    /// </para>
    /// </summary>
    public void Tick(TimeSpan elapsed)
    {
        if (Status != GameStatus.Playing || ActivePiece is null)
        {
            return;
        }

        if (elapsed <= TimeSpan.Zero)
        {
            // EC-10: zero/negative delta is treated as zero; never runs backwards.
            return;
        }

        var clamped = elapsed > MaxElapsedPerTick ? MaxElapsedPerTick : elapsed; // DR-26/EC-2
        var piece = ActivePiece;

        if (piece.IsGrounded(Playfield))
        {
            // FR-016 (US-004's concern): gravity stops acting on a grounded piece. No lock
            // delay is started here — that is story 004.
            return;
        }

        piece.FallTimerElapsed += clamped;

        // DR-14: exactly one cell per elapsed gravityInterval. The while loop lets accumulated
        // time (built up over several prior Tick calls) resolve into as many cell-steps as are
        // due; because a single call is capped to MaxElapsedPerTick (100ms) at level 1's
        // 1.000s interval, a single call can never itself cross more than one threshold.
        while (piece.FallTimerElapsed >= GravityInterval)
        {
            if (piece.IsGrounded(Playfield))
            {
                break;
            }

            var steppedPosition = new CellPosition(piece.Position.Column, piece.Position.Row + 1);
            var definition = TetrominoDefinition.For(piece.Type);
            var steppedCells = definition.SpawnOffsets
                .Select(o => new CellPosition(steppedPosition.Column + o.ColumnOffset, steppedPosition.Row + o.RowOffset));

            if (!Playfield.IsValidPlacement(steppedCells))
            {
                // DR-03/DR-04: never produce an invalid placement, even if groundedness was
                // somehow stale. Stop trying for this tick.
                break;
            }

            piece.Position = steppedPosition;
            piece.FallTimerElapsed -= GravityInterval;
        }
    }
}
