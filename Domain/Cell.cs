namespace infosupport_tetris.Domain;

/// <summary>
/// A single playfield cell. Holds nothing, or the type of the tetromino that locked there
/// (never a reference to the active piece — once locked a block has no identity beyond its
/// type). See spec §5.3.
/// </summary>
public sealed class Cell
{
    public TetrominoType? Occupant { get; set; }

    public bool IsOccupied => Occupant is not null;
}
