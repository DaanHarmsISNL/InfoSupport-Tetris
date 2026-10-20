namespace infosupport_tetris.Domain;

/// <summary>
/// The grid of cells, holding both the empty space and the stack of locked blocks.
/// Always exactly 10 columns by 40 rows (DR-01): rows 0-19 are hidden buffer rows where
/// pieces spawn, rows 20-39 are the visible rows shown to the player. These dimensions
/// never change during a session.
/// </summary>
public sealed class Playfield
{
    public const int ColumnCount = 10;
    public const int VisibleRowCount = 20;
    public const int BufferRowCount = 20;
    public const int TotalRowCount = VisibleRowCount + BufferRowCount; // 40

    /// <summary>Indexed [row][column]. Every position always holds a <see cref="Cell"/>.</summary>
    public Cell[][] Cells { get; }

    public Playfield()
    {
        Cells = new Cell[TotalRowCount][];
        for (var row = 0; row < TotalRowCount; row++)
        {
            Cells[row] = new Cell[ColumnCount];
            for (var column = 0; column < ColumnCount; column++)
            {
                Cells[row][column] = new Cell();
            }
        }
    }

    public Cell GetCell(int row, int column) => Cells[row][column];

    /// <summary>
    /// The cell at a visible-row index (0 = top visible row / row 20, 19 = floor row / row 39).
    /// Used by rendering so the 20 buffer rows are never drawn (DR-01, FR-004).
    /// </summary>
    public Cell GetVisibleCell(int visibleRow, int column) => Cells[BufferRowCount + visibleRow][column];

    /// <summary>
    /// DR-03 (containment) + DR-04 (no overlap): true when every given cell is within bounds
    /// and unoccupied. Together these define a <i>valid placement</i>.
    /// </summary>
    public bool IsValidPlacement(IEnumerable<CellPosition> cells)
    {
        foreach (var cell in cells)
        {
            if (cell.Column < 0 || cell.Column >= ColumnCount || cell.Row < 0 || cell.Row >= TotalRowCount)
            {
                return false;
            }

            if (Cells[cell.Row][cell.Column].IsOccupied)
            {
                return false;
            }
        }

        return true;
    }
}
