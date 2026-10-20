namespace infosupport_tetris.Domain;

/// <summary>
/// The ordered stream of upcoming tetromino types, fed by the 7-bag randomiser (spec §5.1,
/// §5.3). Each bag is a uniformly shuffled permutation of all seven types (DR-09), consumed
/// strictly in bag order and fully before the next is generated (DR-10). The queue always
/// exposes at least seven upcoming types (DR-13), refilling with fresh bags without
/// reordering already-queued types. Seedable for reproducibility (FR-012, NFR-006).
/// </summary>
public sealed class PieceQueue
{
    private const int MinimumUpcoming = 7;

    private readonly Random _random;

    public List<TetrominoType> Upcoming { get; } = [];

    public int? Seed { get; }

    /// <summary>The bag currently being drained into <see cref="Upcoming"/>.</summary>
    public SevenBag CurrentBag { get; private set; }

    public PieceQueue(int? seed = null)
    {
        Seed = seed;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        CurrentBag = new SevenBag(_random);
        RefillIfNeeded();
    }

    /// <summary>
    /// Draws the next type from the front of the queue and refills so at least seven remain
    /// (DR-13, FR-009). Never finds an empty queue (EC-14).
    /// </summary>
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
            if (CurrentBag.IsEmpty)
            {
                CurrentBag = new SevenBag(_random);
            }

            Upcoming.Add(CurrentBag.Draw());
        }
    }
}
