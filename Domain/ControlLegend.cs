namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/014-controls-panel.md §5.3: the display projection of the player's intent, as the
/// player understands it. Deliberately distinct from <see cref="InputCommand"/> (DR-261): where
/// one key produces a held-key start/end pair -- soft drop's <see cref="InputCommand.SoftDropStart"/>
/// / <see cref="InputCommand.SoftDropEnd"/> -- the two collapse into a single <see cref="PlayerAction"/>.
/// Hard drop has no <see cref="InputCommand"/> at all (spec 003/DR-49 models it as an edge trigger
/// posted straight to a dedicated handler), so it is represented here only.
/// </summary>
public enum PlayerAction
{
    MoveLeft,
    MoveRight,
    SoftDrop,
    HardDrop,
    RotateClockwise,
    RotateCounterClockwise,
    Hold
}

/// <summary>
/// docs/specs/014-controls-panel.md §5.2: a single physical key as the player must be able to
/// perceive it. <see cref="Name"/> is the text/accessible form and is never omitted (DR-264);
/// <see cref="Glyph"/> is an optional presentation-only symbol shown alongside it, never instead
/// of it.
/// </summary>
/// <param name="Name">The key's name as text, e.g. "Left arrow". Required, non-empty.</param>
/// <param name="Glyph">An optional symbol shown in place of the name where conventional, e.g. "←".</param>
public sealed record KeyCap(string Name, string? Glyph = null);

/// <summary>
/// docs/specs/014-controls-panel.md §5.2: one row of the control legend -- an action, every key
/// bound to it (DR-263), a short human label, an optional qualifying note (DR-270) and its
/// position in the fixed display order (DR-262). Immutable and derived, never authored by hand
/// per-render (DR-259).
/// </summary>
/// <param name="Action">What the player is doing. One per legend entry (DR-260).</param>
/// <param name="Label">The short human name, e.g. "Rotate clockwise".</param>
/// <param name="Keys">Every key bound to the action, 1-2 entries, in display order (DR-263).</param>
/// <param name="Note">An optional qualifier, e.g. "while held" or "once per piece" (DR-270).</param>
/// <param name="DisplayOrder">Position in the legend, 1-7, unique and stable (DR-262).</param>
public sealed record ControlBinding(
    PlayerAction Action,
    string Label,
    IReadOnlyList<KeyCap> Keys,
    string? Note,
    int DisplayOrder);

/// <summary>
/// docs/specs/014-controls-panel.md §5.1: the ordered, complete projection of the authoritative
/// binding set into something a player can read. Rendered identically by every surface that needs
/// it -- the game screen today, the landing page (story 012) once it exists -- so the two can
/// never disagree (FR-024).
///
/// <para>
/// <b>Single Binding Source (DR-259).</b> This is the <i>only</i> place in the application a key
/// name, glyph or binding list is written down for display purposes. The keys themselves are not
/// re-decided here -- they are the same ones spec 002 §5.3 (<c>KeyBinding</c>, DR-29), spec 003
/// (DR-49) and spec 005 (DR-63) already fix. A known gap: the inline keyboard-handling script in
/// <c>Pages/Game.cshtml</c> is not generated from this class, so its <c>bindings</c> object
/// remains a parallel literal that must match this definition by convention -- enforced by
/// <c>ControlsPanelTests</c> rather than by generation.
/// </para>
/// </summary>
public static class ControlLegend
{
    /// <summary>
    /// The seven legend entries, in <see cref="ControlBinding.DisplayOrder"/> (DR-262): move,
    /// then the two drops, then the two rotations, then hold.
    /// </summary>
    public static IReadOnlyList<ControlBinding> Entries { get; } = new ControlBinding[]
    {
        new(PlayerAction.MoveLeft, "Move left", new[] { new KeyCap("Left arrow", "←") }, null, 1),
        new(PlayerAction.MoveRight, "Move right", new[] { new KeyCap("Right arrow", "→") }, null, 2),
        new(PlayerAction.SoftDrop, "Soft drop", new[] { new KeyCap("Down arrow", "↓") }, "while held", 3),
        new(PlayerAction.HardDrop, "Hard drop", new[] { new KeyCap("Space") }, null, 4),
        new(
            PlayerAction.RotateClockwise,
            "Rotate clockwise",
            new[] { new KeyCap("Up arrow", "↑"), new KeyCap("X") },
            null,
            5),
        new(
            PlayerAction.RotateCounterClockwise,
            "Rotate counter-clockwise",
            new[] { new KeyCap("Z"), new KeyCap("Ctrl") },
            null,
            6),
        new(
            PlayerAction.Hold,
            "Hold piece",
            new[] { new KeyCap("C"), new KeyCap("Shift") },
            "once per piece",
            7),
    };
}

/// <summary>
/// docs/specs/014-controls-panel.md §5.2: one line of the how-to-play summary. Four of them, each
/// summarising exactly one other spec's player-visible behaviour without restating its tables,
/// formulas or numbers (DR-271).
/// </summary>
/// <param name="Order">Reading order, 1-4, unique.</param>
/// <param name="Text">The line as the player reads it.</param>
/// <param name="OwningSpec">The spec whose behaviour this line summarises: "006", "007" or "008".</param>
public sealed record HowToPlayNote(int Order, string Text, string OwningSpec);

/// <summary>
/// docs/specs/014-controls-panel.md §5.2: the proposed copy for the how-to-play summary --
/// exactly four lines (DR-270), each citing its owning spec (DR-271), never restating 006's
/// scoring table, 007's speed curve or 008's top-out conditions.
/// </summary>
public static class HowToPlay
{
    public static IReadOnlyList<HowToPlayNote> Notes { get; } = new HowToPlayNote[]
    {
        new(1, "Fill a row all the way across and it clears, dropping everything above it down.", "006"),
        new(2, "Every cleared row scores — and clearing several at once is worth far more than clearing them one by one.", "006"),
        new(3, "The more lines you clear, the higher your level and the faster the pieces fall.", "007"),
        new(4, "The game ends when there is no room left for the next piece. You will be shown your final score.", "008"),
    };
}

/// <summary>
/// docs/specs/014-controls-panel.md: the data a single render of <c>_ControlsPanel.cshtml</c>
/// needs. Carries no session data of its own (DR-269 -- the panel never reads or writes
/// <see cref="GameSession"/>) beyond the one flag (<see cref="IsPlaying"/>) needed to satisfy
/// DR-268 (no focusable controls while a piece is falling).
/// </summary>
/// <param name="Legend">The seven legend rows, in display order. Defaults to <see cref="ControlLegend.Entries"/>.</param>
/// <param name="HowToPlay">The four how-to-play lines. Defaults to <see cref="infosupport_tetris.Domain.HowToPlay.Notes"/>.</param>
/// <param name="IsPlaying">
/// True while <c>GameSession.Status</c> is <see cref="GameStatus.Playing"/>. When true, the
/// how-to-play collapse control renders non-focusable (DR-268); when false (Ready, GameOver, or
/// no session at all) it is a normal focusable disclosure control.
/// </param>
public sealed record ControlsPanelViewModel(
    IReadOnlyList<ControlBinding> Legend,
    IReadOnlyList<HowToPlayNote> HowToPlay,
    bool IsPlaying)
{
    public static ControlsPanelViewModel For(bool isPlaying) =>
        new(ControlLegend.Entries, infosupport_tetris.Domain.HowToPlay.Notes, isPlaying);
}
