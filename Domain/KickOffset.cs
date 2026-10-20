namespace infosupport_tetris.Domain;

/// <summary>
/// One candidate displacement tried during a rotation, in this project's top-down coordinate
/// convention (spec 002 §5.0): positive <see cref="ColumnOffset"/> is right, positive
/// <see cref="RowOffset"/> is DOWN (a floor kick that lifts the piece is negative).
/// </summary>
public readonly record struct KickOffset(int ColumnOffset, int RowOffset);
