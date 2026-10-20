namespace infosupport_tetris.Domain;

/// <summary>
/// Immutable reference data: one per <see cref="TetrominoType"/>, not session state.
/// Defines the spawn-orientation shapes (DR-05, DR-06) and the Guideline colour mapping
/// (DR-24). See spec §5.3.
/// <para>
/// Story 002 extends this with the Right/Inverted/Left offset tables and SRS wall-kicks —
/// it must not redefine the spawn offsets below.
/// </para>
/// </summary>
public sealed class TetrominoDefinition
{
    public required TetrominoType Type { get; init; }

    /// <summary>Side length of the square rotation box: 4 for I, 2 for O, 3 for T/S/Z/J/L.</summary>
    public required int BoundingBoxSize { get; init; }

    /// <summary>Guideline hex colour (DR-24).</summary>
    public required string Colour { get; init; }

    /// <summary>Occupied cells in <see cref="Orientation.Spawn"/>, exactly four.</summary>
    public required ShapeOffset[] SpawnOffsets { get; init; }

    /// <summary>Width of the occupied cells in Spawn orientation; feeds DR-06.</summary>
    public required int SpawnWidth { get; init; }

    private static readonly Dictionary<TetrominoType, TetrominoDefinition> ByType = new()
    {
        [TetrominoType.I] = new TetrominoDefinition
        {
            Type = TetrominoType.I,
            BoundingBoxSize = 4,
            Colour = "#00FFFF",
            SpawnOffsets = [new(0, 1), new(1, 1), new(2, 1), new(3, 1)],
            SpawnWidth = 4
        },
        [TetrominoType.O] = new TetrominoDefinition
        {
            Type = TetrominoType.O,
            BoundingBoxSize = 2,
            Colour = "#FFFF00",
            SpawnOffsets = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)],
            SpawnWidth = 2
        },
        [TetrominoType.T] = new TetrominoDefinition
        {
            Type = TetrominoType.T,
            BoundingBoxSize = 3,
            Colour = "#800080",
            SpawnOffsets = [new(1, 0), new(0, 1), new(1, 1), new(2, 1)],
            SpawnWidth = 3
        },
        [TetrominoType.S] = new TetrominoDefinition
        {
            Type = TetrominoType.S,
            BoundingBoxSize = 3,
            Colour = "#00FF00",
            SpawnOffsets = [new(1, 0), new(2, 0), new(0, 1), new(1, 1)],
            SpawnWidth = 3
        },
        [TetrominoType.Z] = new TetrominoDefinition
        {
            Type = TetrominoType.Z,
            BoundingBoxSize = 3,
            Colour = "#FF0000",
            SpawnOffsets = [new(0, 0), new(1, 0), new(1, 1), new(2, 1)],
            SpawnWidth = 3
        },
        [TetrominoType.J] = new TetrominoDefinition
        {
            Type = TetrominoType.J,
            BoundingBoxSize = 3,
            Colour = "#0000FF",
            SpawnOffsets = [new(0, 0), new(0, 1), new(1, 1), new(2, 1)],
            SpawnWidth = 3
        },
        [TetrominoType.L] = new TetrominoDefinition
        {
            Type = TetrominoType.L,
            BoundingBoxSize = 3,
            Colour = "#FF7F00",
            SpawnOffsets = [new(2, 0), new(0, 1), new(1, 1), new(2, 1)],
            SpawnWidth = 3
        }
    };

    public static TetrominoDefinition For(TetrominoType type) => ByType[type];
}
