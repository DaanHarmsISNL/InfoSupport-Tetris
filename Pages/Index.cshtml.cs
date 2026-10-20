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
[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
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

    /// <summary>
    /// docs/specs/004-next-piece-preview.md FR-006/SC-002: the preview must already show the
    /// first upcoming type before the player ever presses Start, so a Ready GameSession is
    /// constructed and stored on first visit rather than only on OnPostStart. This adds no new
    /// state beyond what 001 already models (a Ready session with no active piece).
    /// </summary>
    public NextPiecePreview Preview => Session?.NextPiecePreview
        ?? NextPiecePreview.For(new GameSession());

    public void OnGet()
    {
        // Touch the session so the cookie is established even on a first-ever visit, otherwise
        // HttpContext.Session.Id would not be stable across the subsequent POST/redirect.
        HttpContext.Session.SetString("touched", "1");
        Session = _store.Get(SessionId);

        if (Session is null)
        {
            // FR-006: a Ready session must exist before Start is pressed so its preview can be
            // read (EC-1). No gameplay starts here — Status stays Ready until OnPostStart.
            Session = _store.Replace(SessionId, new GameSession());
        }

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

    /// <summary>
    /// Spec 003: hard drop (FR-001..FR-013). The page has no JS framework, so gravity/lock delay
    /// are driven by auto-refresh (see <see cref="OnGet"/>) and this handler is invoked by a
    /// dedicated form the page's edge-triggered keydown script submits on `Space` (DR-49, DR-47).
    /// Catches up gravity/lock-delay time elapsed since the last tick first, exactly as <see cref="OnGet"/>
    /// does, so a hard drop reflects the same state the player is currently looking at. DR-50 is
    /// enforced by <see cref="GameSession.HardDrop"/> itself: a request that arrives with no active
    /// piece or outside <see cref="GameStatus.Playing"/> is simply discarded.
    /// </summary>
    public IActionResult OnPostHardDrop()
    {
        HttpContext.Session.SetString("touched", "1");
        var session = _store.Get(SessionId);

        if (session is { Status: GameStatus.Playing })
        {
            var now = DateTimeOffset.UtcNow;
            if (session.LastTickAt is { } lastTick)
            {
                session.Tick(now - lastTick);
            }

            session.LastTickAt = now;
            session.HardDrop();
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Spec 002 FR-002/DR-29: the handler every bound key's client-side script posts to. Ticks
    /// gravity/soft-drop up to "now" first (DR-43: gravity for elapsed time, then this input) and
    /// applies exactly one <see cref="InputCommand"/>, gated by <see cref="GameSession"/> itself
    /// per DR-28. Returns 204 — the page's own auto-refresh (meta-refresh) re-renders the board;
    /// this endpoint exists only to deliver the command with low latency between refreshes.
    /// </summary>
    public IActionResult OnPostInput(InputCommand command)
    {
        var session = _store.Get(SessionId);
        if (session is null)
        {
            return NotFound();
        }

        if (session.Status == GameStatus.Playing)
        {
            var now = DateTimeOffset.UtcNow;
            if (session.LastTickAt is { } lastTick)
            {
                session.Tick(now - lastTick);
            }

            session.LastTickAt = now;
        }

        switch (command)
        {
            case InputCommand.MoveLeft:
                session.PressLeft();
                break;
            case InputCommand.MoveRight:
                session.PressRight();
                break;
            case InputCommand.RotateClockwise:
                session.RotateClockwise();
                break;
            case InputCommand.RotateCounterClockwise:
                session.RotateCounterClockwise();
                break;
            case InputCommand.SoftDropStart:
                session.PressSoftDrop();
                break;
            case InputCommand.SoftDropEnd:
                session.ReleaseSoftDrop();
                break;
        }

        return new NoContentResult();
    }

    /// <summary>Spec 002 DR-31/DR-32/EC-7/EC-8: a direction key was released (or focus was lost,
    /// in which case the client releases both).</summary>
    public IActionResult OnPostRelease(InputCommand command)
    {
        var session = _store.Get(SessionId);
        if (session is null)
        {
            return NotFound();
        }

        switch (command)
        {
            case InputCommand.MoveLeft:
                session.ReleaseLeft();
                break;
            case InputCommand.MoveRight:
                session.ReleaseRight();
                break;
            case InputCommand.SoftDropEnd:
                session.ReleaseSoftDrop();
                break;
        }

        return new NoContentResult();
    }

    private string SessionId => HttpContext.Session.Id;
}
