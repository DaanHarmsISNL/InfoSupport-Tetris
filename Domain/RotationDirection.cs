namespace infosupport_tetris.Domain;

/// <summary>
/// Which way a rotation key turns the piece (spec 002 §5.3). Not an <see cref="Orientation"/>
/// itself — it is the verb applied to DR-33's cycle.
/// </summary>
public enum RotationDirection
{
    Clockwise,
    CounterClockwise
}
