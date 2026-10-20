namespace infosupport_tetris.Domain;

/// <summary>
/// Keyboard state for one <see cref="GameSession"/> (spec 002 §5.1). Holds only what DAS/ARR
/// auto-repeat and soft drop need between update steps; never persisted, never part of the
/// playfield. Reset on construction, on *New game* and on reaching <see cref="GameStatus.GameOver"/>
/// (DR-28) — except <see cref="HeldDirections"/> and <see cref="SoftDropHeld"/> are deliberately
/// left alone across a lock, per DR-31/EC-5: a key the player still holds keeps acting on the
/// next piece.
/// </summary>
public sealed class InputState
{
    public static readonly TimeSpan DasDelay = TimeSpan.FromMilliseconds(167);
    public static readonly TimeSpan ArrInterval = TimeSpan.FromMilliseconds(33);
    public const int SoftDropFactor = 20;

    /// <summary>DR-32: ordered oldest-first; the last element is the active auto-repeat direction.</summary>
    public List<HorizontalDirection> HeldDirections { get; } = [];

    public HorizontalDirection? ActiveDirection => HeldDirections.Count == 0 ? null : HeldDirections[^1];

    public TimeSpan? DasTimerElapsed { get; private set; }

    public bool AutoRepeatArmed { get; private set; }

    public TimeSpan? ArrTimerElapsed { get; private set; }

    public bool SoftDropHeld { get; set; }

    /// <summary>DR-32: a direction key is pressed. Becomes/stays the active direction and
    /// restarts the DAS delay; the caller still has to perform the immediate move itself.</summary>
    public void PressDirection(HorizontalDirection direction)
    {
        HeldDirections.Remove(direction);
        HeldDirections.Add(direction);
        RestartDas();
    }

    /// <summary>DR-31/DR-32: releasing a key clears its auto-repeat state. If it was the active
    /// direction and another is still held, that one becomes active with a fresh DAS delay —
    /// the caller performs its immediate move.</summary>
    public void ReleaseDirection(HorizontalDirection direction)
    {
        HeldDirections.Remove(direction);

        if (HeldDirections.Count == 0)
        {
            DasTimerElapsed = null;
            AutoRepeatArmed = false;
            ArrTimerElapsed = null;
            return;
        }

        RestartDas();
    }

    private void RestartDas()
    {
        DasTimerElapsed = TimeSpan.Zero;
        AutoRepeatArmed = false;
        ArrTimerElapsed = null;
    }

    /// <summary>
    /// DR-31: advances the DAS/ARR timers by <paramref name="elapsed"/> and reports how many
    /// auto-repeat moves are due in the active direction for this step. The immediate move on
    /// first press is the caller's responsibility (it happens on <see cref="PressDirection"/>,
    /// not here) — this only ever reports *repeat* moves once DAS has elapsed.
    /// </summary>
    public int AdvanceAndCountRepeats(TimeSpan elapsed)
    {
        if (ActiveDirection is null || DasTimerElapsed is null)
        {
            return 0;
        }

        if (!AutoRepeatArmed)
        {
            DasTimerElapsed += elapsed;
            if (DasTimerElapsed < DasDelay)
            {
                return 0;
            }

            AutoRepeatArmed = true;
            ArrTimerElapsed = DasTimerElapsed.Value - DasDelay; // EC-9: no time is lost at the boundary.
        }
        else
        {
            ArrTimerElapsed += elapsed;
        }

        var repeats = 0;
        while (ArrTimerElapsed >= ArrInterval)
        {
            ArrTimerElapsed -= ArrInterval;
            repeats++;
        }

        return repeats;
    }

    /// <summary>EC-7/EC-8: focus loss releases everything — as if every held key were released.</summary>
    public void ReleaseAll()
    {
        HeldDirections.Clear();
        DasTimerElapsed = null;
        AutoRepeatArmed = false;
        ArrTimerElapsed = null;
        SoftDropHeld = false;
    }

    /// <summary>DR-28: full reset on session construction, *New game*, and reaching GameOver.</summary>
    public void Reset() => ReleaseAll();
}
