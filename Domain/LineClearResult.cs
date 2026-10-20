namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/006-line-clears-and-scoring.md §5.3: the outcome of one lock resolution.
/// Immutable once produced. A <see cref="ClearType"/> of <see cref="ClearType.None"/> is still a
/// valid, recorded result (EC-1) — it simply carries 0 cleared rows and 0 points.
/// </summary>
/// <param name="ClearedRows">
/// Row indices (0-39) that were full, as they stood <i>before</i> the collapse (DR-77). Strictly
/// ascending, 0-4 entries.
/// </param>
/// <param name="ClearType">Derived from <paramref name="ClearedRows"/>.Count (DR-79).</param>
/// <param name="LevelAtClear">The <see cref="GameSession.Level"/> used as the multiplier (DR-84).</param>
/// <param name="PointsAwarded">0 when <paramref name="ClearType"/> is <see cref="Domain.ClearType.None"/> (DR-80).</param>
public sealed record LineClearResult(
    IReadOnlyList<int> ClearedRows,
    ClearType ClearType,
    int LevelAtClear,
    long PointsAwarded)
{
    /// <summary>DR-87: how many rows this lock cleared, 0-4.</summary>
    public int ClearCount => ClearedRows.Count;

    /// <summary>DR-80: 0/100/300/500/800 for None/Single/Double/Triple/Tetris, before the level multiplier.</summary>
    public static int BasePoints(ClearType clearType) => clearType switch
    {
        ClearType.None => 0,
        ClearType.Single => 100,
        ClearType.Double => 300,
        ClearType.Triple => 500,
        ClearType.Tetris => 800,
        _ => throw new ArgumentOutOfRangeException(nameof(clearType))
    };

    /// <summary>DR-79: 0/1/2/3/4 cleared rows map one-to-one to a <see cref="Domain.ClearType"/>.</summary>
    public static ClearType ClassifyClearCount(int clearCount) => clearCount switch
    {
        0 => ClearType.None,
        1 => ClearType.Single,
        2 => ClearType.Double,
        3 => ClearType.Triple,
        4 => ClearType.Tetris,
        _ => throw new ArgumentOutOfRangeException(nameof(clearCount), "DR-87: a single lock clears 0-4 rows.")
    };

    /// <summary>Builds the result for a resolved lock, deriving <see cref="ClearType"/> and
    /// <see cref="PointsAwarded"/> from <paramref name="clearedRows"/> and <paramref name="level"/>.</summary>
    public static LineClearResult For(IReadOnlyList<int> clearedRows, int level)
    {
        var clearType = ClassifyClearCount(clearedRows.Count);
        var points = (long)BasePoints(clearType) * level;
        return new LineClearResult(clearedRows, clearType, level, points);
    }
}
