using System.Linq;

namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/005-hold-piece.md DR-73: the player-facing projection of the hold slot, beside the
/// playfield. Like <see cref="NextPiecePreview"/>, this is a read-only presentation helper, not a
/// new domain entity (§5.1 of the spec explicitly adds no entity beyond two fields on
/// <see cref="GameSession"/>) -- every member here is a pure function of
/// <see cref="GameSession.HeldType"/> and <see cref="GameSession.HoldAvailable"/>, recomputed on
/// every access, never cached.
/// </summary>
public sealed class HoldDisplay
{
    /// <summary>DR-73: the hold area is the same fixed 4x2 footprint as story 004's preview grid.</summary>
    public const int ColumnCount = 4;

    public const int RowCount = 2;

    /// <summary>The held type, or null when the slot is empty.</summary>
    public TetrominoType? HeldType { get; }

    /// <summary>DR-73: always Spawn, regardless of the orientation the piece had when it was held.</summary>
    public Orientation Orientation => Orientation.Spawn;

    /// <summary>DR-73/DR-24: the same colour the type has as the ActivePiece and once locked; null when empty.</summary>
    public string? Colour { get; }

    /// <summary>DR-73/DR-58-style centring: the held shape's occupied cells, centred within the 4x2 grid.</summary>
    public IReadOnlyList<PreviewCell> Cells { get; }

    /// <summary>FR-016/DR-73: persistent label identifying the area.</summary>
    public string Caption => "Hold";

    /// <summary>
    /// FR-018/DR-73: whether hold is currently usable (mirrors <see cref="GameSession.HoldAvailable"/>).
    /// Drives the dimmed/unavailable rendering -- also true/false when the slot is empty, since an
    /// empty slot in `Ready`/`GameOver` is itself an "unavailable" presentation (EC-19).
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>Non-colour, non-shape text alternative naming the held type (consistent with DR-59's pattern).</summary>
    public string AccessibleName { get; }

    private HoldDisplay(TetrominoType? heldType, string? colour, IReadOnlyList<PreviewCell> cells, bool isAvailable, string accessibleName)
    {
        HeldType = heldType;
        Colour = colour;
        Cells = cells;
        IsAvailable = isAvailable;
        AccessibleName = accessibleName;
    }

    /// <summary>
    /// Derives the hold display from <paramref name="session"/>'s current state. Reads only
    /// <see cref="GameSession.HeldType"/> and <see cref="GameSession.HoldAvailable"/> -- never
    /// mutates anything (DR-61-style: display-only).
    /// </summary>
    public static HoldDisplay For(GameSession session)
    {
        var heldType = session.HeldType;
        var isAvailable = session.HoldAvailable;

        if (heldType is null)
        {
            return new HoldDisplay(null, null, [], isAvailable, "Hold: empty");
        }

        var definition = TetrominoDefinition.For(heldType.Value);
        var offsets = definition.SpawnOffsets;

        var minColumn = offsets.Min(o => o.ColumnOffset);
        var maxColumn = offsets.Max(o => o.ColumnOffset);
        var minRow = offsets.Min(o => o.RowOffset);
        var maxRow = offsets.Max(o => o.RowOffset);

        var occupiedWidth = maxColumn - minColumn + 1;
        var occupiedHeight = maxRow - minRow + 1;

        var horizontalInset = (ColumnCount - occupiedWidth) / 2.0;
        var verticalInset = (RowCount - occupiedHeight) / 2.0;

        var cells = offsets
            .Select(o => new PreviewCell(
                horizontalInset + (o.ColumnOffset - minColumn),
                verticalInset + (o.RowOffset - minRow)))
            .ToArray();

        return new HoldDisplay(heldType, definition.Colour, cells, isAvailable, $"Hold: {heldType}");
    }
}
