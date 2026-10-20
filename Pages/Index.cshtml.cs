using infosupport_tetris.Domain;
using infosupport_tetris.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace infosupport_tetris.Pages;

/// <summary>
/// US-001: Start playing immediately. Implements FR-001 (start affordance, no sign-up),
/// FR-002 (fresh GameSession), FR-003/FR-004 (render only the 20 visible rows), and FR-005
/// (New game discards and restarts, DR-23, EC-8 — no confirmation prompt).
/// Spawning, gravity, locking, scoring and game-over presentation are out of scope (see
/// docs/specs/001-start-new-game.md §1.3) and are left to stories 002-008.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IGameSessionStore _store;

    public IndexModel(IGameSessionStore store)
    {
        _store = store;
    }

    public GameSession? Session { get; private set; }

    /// <summary>
    /// US-002 (FR-010..FR-012): the active piece's occupied cells restricted to the 20 visible
    /// rows and colour-coded by its TetrominoType (DR-24), keyed by (visibleRow, column).
    /// Buffer-row cells (row &lt; Playfield.BufferRowCount) are omitted so they stay invisible
    /// per DR-01/FR-004.
    /// </summary>
    public IReadOnlyDictionary<(int VisibleRow, int Column), string> ActivePieceVisibleCells
    {
        get
        {
            var cells = new Dictionary<(int, int), string>();
            if (Session?.ActivePiece is { } activePiece)
            {
                var colour = TetrominoDefinition.For(activePiece.Type).Colour;
                foreach (var cell in activePiece.OccupiedCells)
                {
                    if (cell.Row >= Playfield.BufferRowCount)
                    {
                        cells[(cell.Row - Playfield.BufferRowCount, cell.Column)] = colour;
                    }
                }
            }

            return cells;
        }
    }

    public void OnGet()
    {
        // Touch the session so the cookie is established even on a first-ever visit, otherwise
        // HttpContext.Session.Id would not be stable across the subsequent POST/redirect.
        HttpContext.Session.SetString("touched", "1");
        Session = _store.Get(SessionId);

        // US-003 (DR-14/DR-26): minimal game-loop wiring. There is no JS framework yet, so the
        // page auto-refreshes (see the meta-refresh tag in Index.cshtml) and each GET drives
        // gravity from the real wall-clock time elapsed since the previous tick. This keeps the
        // spec-tested logic entirely in GameSession.Tick, which is exercised deterministically
        // by the test suite via simulated deltas rather than real elapsed time.
        if (Session is { Status: GameStatus.Playing })
        {
            var now = DateTimeOffset.UtcNow;
            if (Session.LastTickAt is { } lastTick)
            {
                Session.Tick(now - lastTick);
            }

            Session.LastTickAt = now;
        }
    }

    /// <summary>FR-001: single action, no data entry, Ready -&gt; Playing.</summary>
    public IActionResult OnPostStart()
    {
        HttpContext.Session.SetString("touched", "1");
        var session = new GameSession();
        session.Start(DateTimeOffset.UtcNow);
        _store.Replace(SessionId, session);
        return RedirectToPage();
    }

    /// <summary>FR-005/DR-23/EC-8: discard whatever is there (if anything) and start fresh, no confirmation.</summary>
    public IActionResult OnPostNewGame()
    {
        HttpContext.Session.SetString("touched", "1");
        var session = new GameSession();
        session.Start(DateTimeOffset.UtcNow);
        _store.Replace(SessionId, session);
        return RedirectToPage();
    }

    private string SessionId => HttpContext.Session.Id;
}
