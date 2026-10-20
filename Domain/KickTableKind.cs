namespace infosupport_tetris.Domain;

/// <summary>
/// Selects which SRS wall-kick table a <see cref="TetrominoDefinition"/> uses (spec 002 §5.3).
/// </summary>
public enum KickTableKind
{
    /// <summary>Shared table used by J, L, S, T and Z.</summary>
    JLSTZ,

    /// <summary>The I tetromino's own, wider table.</summary>
    I,

    /// <summary>O never kicks — rotation is a no-op in position (DR-36).</summary>
    None
}
