namespace infosupport_tetris.Domain;

/// <summary>
/// The ordered stream of upcoming tetromino types, fed by the 7-bag randomiser (spec §5.1).
/// <para>
/// STUB: the real 7-bag randomiser (DR-09, DR-10, DR-13) is story 002's responsibility (US-002).
/// This spec (001/US-001) only needs <see cref="GameSession.PieceQueue"/> to exist as a field so
/// later work can fill it in. To keep SC-002's "queue holds at least seven upcoming types"
/// assertion meaningful without implementing the bag algorithm, this stub seeds itself with a
/// fixed, non-random cycle of all seven types repeated. Story 002 should replace the seeding
/// logic in the constructor with a real <c>SevenBag</c>-backed implementation; the public shape
/// (<see cref="Upcoming"/>, <see cref="Draw"/>) is intended to remain stable.
/// </para>
/// </summary>
public sealed class PieceQueue
{
    private const int MinimumUpcoming = 7;

    private static readonly TetrominoType[] AllTypes =
    [
        TetrominoType.I, TetrominoType.O, TetrominoType.T,
        TetrominoType.S, TetrominoType.Z, TetrominoType.J, TetrominoType.L
    ];

    public List<TetrominoType> Upcoming { get; } = [];

    public int? Seed { get; }

    public PieceQueue(int? seed = null)
    {
        Seed = seed;
        RefillIfNeeded();
    }

    /// <summary>Draws the next type and refills so at least seven remain (DR-13). Not used by US-001.</summary>
    public TetrominoType Draw()
    {
        var next = Upcoming[0];
        Upcoming.RemoveAt(0);
        RefillIfNeeded();
        return next;
    }

    private void RefillIfNeeded()
    {
        while (Upcoming.Count < MinimumUpcoming)
        {
            Upcoming.AddRange(AllTypes);
        }
    }
}
