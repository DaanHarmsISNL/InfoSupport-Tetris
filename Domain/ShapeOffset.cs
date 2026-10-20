namespace infosupport_tetris.Domain;

/// <summary>
/// A single cell of a tetromino, relative to its bounding box top-left. See spec §5.3.
/// </summary>
public readonly record struct ShapeOffset(int ColumnOffset, int RowOffset);
