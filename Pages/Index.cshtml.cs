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

    public void OnGet()
    {
        // Touch the session so the cookie is established even on a first-ever visit, otherwise
        // HttpContext.Session.Id would not be stable across the subsequent POST/redirect.
        HttpContext.Session.SetString("touched", "1");
        Session = _store.Get(SessionId);
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
