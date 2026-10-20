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

    /// <summary>
    /// docs/specs/006-line-clears-and-scoring.md DR-75/DR-76: scans all 40 rows (buffer and
    /// visible alike) and returns the indices of every full row, in ascending order. A row is
    /// full exactly when all 10 of its cells are occupied.
    /// </summary>
    public IReadOnlyList<int> DetectFullRows()
    {
        var fullRows = new List<int>();
        for (var row = 0; row < TotalRowCount; row++)
        {
            var isFull = true;
            for (var column = 0; column < ColumnCount; column++)
            {
                if (!Cells[row][column].IsOccupied)
                {
                    isFull = false;
                    break;
                }
            }

            if (isFull)
            {
                fullRows.Add(row);
            }
        }

        return fullRows;
    }

    /// <summary>
    /// DR-77 — Row Collapse: removes every row in <paramref name="clearedRows"/> and shifts each
    /// surviving row down by the number of cleared rows below it, as a single simultaneous
    /// remapping. The topmost <c>clearedRows.Count</c> rows end up entirely empty. No block ever
    /// changes column or moves upward (verified by SC-004/SC-005: the non-adjacent case). A no-op
    /// when <paramref name="clearedRows"/> is empty (DR-78: no cascade/re-settling happens here or
    /// ever).
    /// </summary>
    public void CollapseRows(IReadOnlyList<int> clearedRows)
    {
        if (clearedRows.Count == 0)
        {
            return;
        }

        var cleared = new HashSet<int>(clearedRows);

        // Build the new grid by, for every destination row from the bottom up, finding the
        // surviving source row that maps onto it. Working bottom-to-top lets each destination
        // row simply consume the next not-yet-placed surviving row above it (and above any
        // already-consumed rows), honoring DR-77's "shift down by cleared rows below it" rule.
        var newRows = new Cell[TotalRowCount][];
        var sourceRow = TotalRowCount - 1;
        for (var destRow = TotalRowCount - 1; destRow >= 0; destRow--)
        {
            while (sourceRow >= 0 && cleared.Contains(sourceRow))
            {
                sourceRow--;
            }

            if (sourceRow >= 0)
            {
                newRows[destRow] = Cells[sourceRow];
                sourceRow--;
            }
            else
            {
                // Topmost rows left after surviving rows run out become entirely empty.
                var emptyRow = new Cell[ColumnCount];
                for (var column = 0; column < ColumnCount; column++)
                {
                    emptyRow[column] = new Cell();
                }

                newRows[destRow] = emptyRow;
            }
        }

        for (var row = 0; row < TotalRowCount; row++)
        {
            Cells[row] = newRows[row];
        }
    }
}
