namespace infosupport_tetris.Domain;

/// <summary>
/// One shuffled bag in the 7-bag randomiser. Created full (a uniformly shuffled permutation of
/// all seven <see cref="TetrominoType"/> values, DR-09), drained from the front, and discarded
/// when empty (DR-10). See spec §5.3.
/// </summary>
public sealed class SevenBag
{
    private static readonly TetrominoType[] AllTypes =
    [
        TetrominoType.I, TetrominoType.O, TetrominoType.T,
        TetrominoType.S, TetrominoType.Z, TetrominoType.J, TetrominoType.L
    ];

    private readonly List<TetrominoType> _remaining;

    public IReadOnlyList<TetrominoType> Remaining => _remaining;

    public bool IsEmpty => _remaining.Count == 0;

    public SevenBag(Random random)
    {
        _remaining = [.. AllTypes];

        // Unbiased Fisher-Yates shuffle (DR-09): all 5040 permutations equally likely.
        for (var i = _remaining.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (_remaining[i], _remaining[j]) = (_remaining[j], _remaining[i]);
        }
    }

    /// <summary>Draws the next type in bag order (DR-10). Throws if the bag is empty.</summary>
    public TetrominoType Draw()
    {
        if (_remaining.Count == 0)
        {
            throw new InvalidOperationException("Cannot draw from an empty SevenBag.");
        }

        var next = _remaining[0];
        _remaining.RemoveAt(0);
        return next;
    }
}
