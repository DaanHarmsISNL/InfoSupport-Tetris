namespace infosupport_tetris.Domain;

/// <summary>
/// A zero-based (column, row) coordinate. Origin is top-left; row increases downward
/// (gravity increases row). See spec §5.0 for the full coordinate convention and §5.3
/// for this value object's definition.
/// </summary>
public readonly record struct CellPosition(int Column, int Row);
