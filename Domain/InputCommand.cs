namespace infosupport_tetris.Domain;

/// <summary>
/// The command a key press or release resolves to (spec 002 §5.3). Exists so DR-43's
/// "apply input before gravity, in arrival order" rule has something concrete to order.
/// </summary>
public enum InputCommand
{
    MoveLeft,
    MoveRight,
    RotateClockwise,
    RotateCounterClockwise,
    SoftDropStart,
    SoftDropEnd
}
