namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/006-line-clears-and-scoring.md §5.3: a single addition to <see cref="GameSession.Score"/>.
/// Modelled explicitly so that Score Monotonicity and Range (DR-83) is structural — score is the
/// sum of awards and there is no operation that subtracts.
/// </summary>
/// <param name="Points">Never negative (DR-83). An award of 0 is legal and a no-op on score.</param>
/// <param name="Source">Which rule produced it (DR-80/DR-81).</param>
/// <param name="CellCount">
/// Cells descended (soft drop) or travelled (hard drop), for traceability and testing. Null for
/// <see cref="ScoreSource.LineClear"/>.
/// </param>
public sealed record ScoreAward(long Points, ScoreSource Source, int? CellCount = null);
