namespace infosupport_tetris.Domain;

/// <summary>
/// Spec 003 §5.3 (DR-48): published at the moment a hard-dropped piece locks. Carries the
/// travelled-cell count for story 006's scoring (2 points per cell) and the piece identity/landing
/// position for story 006's/008's reporting. This spec (003) reads nothing back from it and adds
/// nothing to <see cref="GameSession.Score"/> itself.
/// </summary>
/// <param name="CellsTravelled">
/// The <c>d</c> of DR-44: how many cells the piece moved down before locking. 0 for a piece that
/// was already grounded (DR-45/EC-3).
/// </param>
/// <param name="LandingPosition">Where the piece locked (same convention as <see cref="ActivePiece.Position"/>).</param>
/// <param name="Type">Which tetromino type was dropped.</param>
public sealed record HardDropOutcome(int CellsTravelled, CellPosition LandingPosition, TetrominoType Type);
