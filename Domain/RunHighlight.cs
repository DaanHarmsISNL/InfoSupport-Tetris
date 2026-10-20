namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/011-highlight-my-run.md §5.4: which of the three presentations applies to a
/// highlight, derived from the entry's rank and story 010's default/current view -- never chosen
/// by a caller.
/// </summary>
public enum HighlightPlacementCase
{
    /// <summary>The row is already shown; highlight it in place, add nothing (DR-211).</summary>
    WithinDefaultView,

    /// <summary>The row sits below what is shown; highlight it and append a <see cref="PlacementContext"/> (DR-212).</summary>
    OutsideDefaultView,
}

/// <summary>
/// docs/specs/011-highlight-my-run.md §5.3: where one entry stood in the board as it was last
/// read. Immutable -- a later read produces a new <see cref="Placement"/>, never mutates this one
/// (DR-209).
/// </summary>
public sealed record Placement(int Rank, int TotalEntries, DateTimeOffset ReadAt, HighlightPlacementCase Case)
{
    /// <summary>FR-006/DR-208: "You placed 47th of 183."</summary>
    public string Statement => $"You placed {Ordinal(Rank)} of {TotalEntries}";

    private static string Ordinal(int n)
    {
        if (n % 100 is >= 11 and <= 13)
        {
            return $"{n}th";
        }

        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th",
        };
    }
}

/// <summary>
/// docs/specs/011-highlight-my-run.md §5.3: the small slice of the board shown below the default
/// view so an out-of-view row is reachable without hunting. Present only when
/// <see cref="Placement.Case"/> is <see cref="HighlightPlacementCase.OutsideDefaultView"/> (DR-212).
/// </summary>
/// <param name="Rows">The player's row and up to two nearest-ranked neighbours each side, in rank order (DR-212).</param>
/// <param name="ShowsBreak">Whether a labelled break separates this slice from the default view above it (DR-213.2).</param>
public sealed record PlacementContext(IReadOnlyList<RankedEntry> Rows, bool ShowsBreak);

/// <summary>
/// docs/specs/011-highlight-my-run.md §5.1: the client's answer to "which row is mine, and where
/// did I land". Transient and never persisted (DR-218) -- this type only carries the result of
/// resolving one entryId against one reading of story 010's board; it is not stored anywhere.
/// </summary>
public sealed record RunHighlight(Guid EntryId, Placement Placement, PlacementContext? Context);
