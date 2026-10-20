namespace infosupport_tetris.Domain;

/// <summary>
/// Immutable reference data: the SRS wall-kick tables spec 001 deferred to spec 002 §5.3.
/// Selected by <see cref="TetrominoDefinition.KickTableKind"/>. Already converted to this
/// project's top-down row convention (spec 002 §5.0) — do not negate these a second time.
/// </summary>
public static class KickTable
{
    /// <summary>The eight legal quarter-turn transitions, keyed <c>(from, to)</c>. DR-33: there
    /// is no 180° rotation, so <c>Spawn↔Inverted</c> and <c>Right↔Left</c> are never keys here.</summary>
    private static readonly Dictionary<(Orientation From, Orientation To), KickOffset[]> JlstzRows = new()
    {
        [(Orientation.Spawn, Orientation.Right)] =
            [new(0, 0), new(-1, 0), new(-1, -1), new(0, 2), new(-1, 2)],
        [(Orientation.Right, Orientation.Spawn)] =
            [new(0, 0), new(1, 0), new(1, 1), new(0, -2), new(1, -2)],
        [(Orientation.Right, Orientation.Inverted)] =
            [new(0, 0), new(1, 0), new(1, 1), new(0, -2), new(1, -2)],
        [(Orientation.Inverted, Orientation.Right)] =
            [new(0, 0), new(-1, 0), new(-1, -1), new(0, 2), new(-1, 2)],
        [(Orientation.Inverted, Orientation.Left)] =
            [new(0, 0), new(1, 0), new(1, -1), new(0, 2), new(1, 2)],
        [(Orientation.Left, Orientation.Inverted)] =
            [new(0, 0), new(-1, 0), new(-1, 1), new(0, -2), new(-1, -2)],
        [(Orientation.Left, Orientation.Spawn)] =
            [new(0, 0), new(-1, 0), new(-1, 1), new(0, -2), new(-1, -2)],
        [(Orientation.Spawn, Orientation.Left)] =
            [new(0, 0), new(1, 0), new(1, -1), new(0, 2), new(1, 2)]
    };

    private static readonly Dictionary<(Orientation From, Orientation To), KickOffset[]> IRows = new()
    {
        [(Orientation.Spawn, Orientation.Right)] =
            [new(0, 0), new(-2, 0), new(1, 0), new(-2, 1), new(1, -2)],
        [(Orientation.Right, Orientation.Spawn)] =
            [new(0, 0), new(2, 0), new(-1, 0), new(2, -1), new(-1, 2)],
        [(Orientation.Right, Orientation.Inverted)] =
            [new(0, 0), new(-1, 0), new(2, 0), new(-1, -2), new(2, 1)],
        [(Orientation.Inverted, Orientation.Right)] =
            [new(0, 0), new(1, 0), new(-2, 0), new(1, 2), new(-2, -1)],
        [(Orientation.Inverted, Orientation.Left)] =
            [new(0, 0), new(2, 0), new(-1, 0), new(2, -1), new(-1, 2)],
        [(Orientation.Left, Orientation.Inverted)] =
            [new(0, 0), new(-2, 0), new(1, 0), new(-2, 1), new(1, -2)],
        [(Orientation.Left, Orientation.Spawn)] =
            [new(0, 0), new(1, 0), new(-2, 0), new(1, 2), new(-2, -1)],
        [(Orientation.Spawn, Orientation.Left)] =
            [new(0, 0), new(-1, 0), new(2, 0), new(-1, -2), new(2, 1)]
    };

    /// <summary>
    /// DR-35: the five ordered kick tests for a rotation from <paramref name="from"/> to
    /// <paramref name="to"/>, selected by <paramref name="kind"/>. Returns an empty span for
    /// <see cref="KickTableKind.None"/> (O never kicks, DR-36).
    /// </summary>
    public static IReadOnlyList<KickOffset> RowFor(KickTableKind kind, Orientation from, Orientation to)
    {
        return kind switch
        {
            KickTableKind.JLSTZ => JlstzRows[(from, to)],
            KickTableKind.I => IRows[(from, to)],
            KickTableKind.None => [],
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
