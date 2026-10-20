using System.Linq;

namespace infosupport_tetris.Domain;

/// <summary>
/// One occupied cell of the previewed shape, positioned in preview-grid coordinates (DR-58).
/// Column/Row are measured from the top-left of the 4x2 <see cref="NextPiecePreview"/> grid, in
/// cell units; a value of 0.5 means "half a cell inset" (see spec §5.3 "Derived preview
/// geometry" — this is how the I's vertical centring and the O's horizontal centring are
/// expressed without needing a non-integer grid).
/// </summary>
public readonly record struct PreviewCell(double Column, double Row);

/// <summary>
/// docs/specs/004-next-piece-preview.md: a read-only, derived projection over the front of
/// <see cref="PieceQueue.Upcoming"/> (DR-52). This type has no identity, is never stored and
/// never mutates <see cref="GameSession"/>, <see cref="PieceQueue"/> or <see cref="SevenBag"/>
/// (DR-53) — every member is a pure function of the <see cref="GameSession"/> passed to
/// <see cref="For"/>, recomputed on every call.
/// </summary>
public sealed class NextPiecePreview
{
    /// <summary>DR-57: the preview area is always exactly 4 columns...</summary>
    public const int ColumnCount = 4;

    /// <summary>DR-57: ...by 2 rows, regardless of the previewed type or an empty preview.</summary>
    public const int RowCount = 2;

    /// <summary>DR-52: the type the next draw from the queue will yield; null at/after GameOver (DR-60).</summary>
    public TetrominoType? PreviewedType { get; }

    /// <summary>DR-56: always Spawn, regardless of the ActivePiece's current orientation.</summary>
    public Orientation Orientation => Orientation.Spawn;

    /// <summary>DR-56/DR-24: the same colour the type has as the ActivePiece and once locked; null when empty.</summary>
    public string? Colour { get; }

    /// <summary>
    /// DR-56/DR-58: the previewed shape's occupied cells, already centred within the 4x2 grid.
    /// Empty when <see cref="PreviewedType"/> is null.
    /// </summary>
    public IReadOnlyList<PreviewCell> Cells { get; }

    /// <summary>FR-014: persistent label identifying the area, whether or not it is populated.</summary>
    public string Caption => "Next";

    /// <summary>DR-59/FR-013: a non-colour, non-shape text alternative naming the previewed type.</summary>
    public string AccessibleName { get; }

    private NextPiecePreview(TetrominoType? previewedType, string? colour, IReadOnlyList<PreviewCell> cells, string accessibleName)
    {
        PreviewedType = previewedType;
        Colour = colour;
        Cells = cells;
        AccessibleName = accessibleName;
    }

    /// <summary>
    /// Derives the preview from <paramref name="session"/>'s current state. Reads only
    /// <see cref="GameSession.Status"/> and <see cref="PieceQueue.Upcoming"/>[0] (DR-52) — never
    /// <see cref="PieceQueue.Draw"/>, never any other index, never <see cref="PieceQueue.CurrentBag"/>
    /// (DR-53). Safe to call at any point in the session's lifetime, including while it is still
    /// <see cref="GameStatus.Ready"/> (FR-006/EC-1) and after <see cref="GameStatus.GameOver"/>
    /// (DR-60/EC-7/EC-8).
    /// </summary>
    public static NextPiecePreview For(GameSession session)
    {
        if (session.Status == GameStatus.GameOver)
        {
            return Empty();
        }

        var upcoming = session.PieceQueue.Upcoming;
        if (upcoming.Count == 0)
        {
            // EC-12: defensive only — DR-13 guarantees this never actually happens.
            return Empty();
        }

        var type = upcoming[0];
        return ForType(type);
    }

    private static NextPiecePreview ForType(TetrominoType type)
    {
        var definition = TetrominoDefinition.For(type);
        var offsets = definition.SpawnOffsets;

        var minColumn = offsets.Min(o => o.ColumnOffset);
        var maxColumn = offsets.Max(o => o.ColumnOffset);
        var minRow = offsets.Min(o => o.RowOffset);
        var maxRow = offsets.Max(o => o.RowOffset);

        var occupiedWidth = maxColumn - minColumn + 1;
        var occupiedHeight = maxRow - minRow + 1;

        // DR-58: centre the occupied-cell bounding box, not the rotation bounding box, within
        // the fixed 4x2 grid (DR-57). See spec §5.3 for the expected per-type inset values.
        var horizontalInset = (ColumnCount - occupiedWidth) / 2.0;
        var verticalInset = (RowCount - occupiedHeight) / 2.0;

        var cells = offsets
            .Select(o => new PreviewCell(
                horizontalInset + (o.ColumnOffset - minColumn),
                verticalInset + (o.RowOffset - minRow)))
            .ToArray();

        return new NextPiecePreview(type, definition.Colour, cells, $"Next piece: {type}");
    }

    private static NextPiecePreview Empty() =>
        new(previewedType: null, colour: null, cells: [], accessibleName: "Next piece: none");
}
