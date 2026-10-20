using infosupport_tetris.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace infosupport_tetris.Pages;

/// <summary>
/// docs/specs/012-landing-page.md: the product's front door. Reads and stores nothing (DR-253) --
/// there is no `GameSession`, no leaderboard read and no other state on this page. The primary
/// action is a plain HTML form that posts straight to <c>GameModel.OnPostStart</c> on
/// <c>/Game</c> (DR-229, FR-025), so activating it works with script disabled and needs no
/// handler of its own here.
/// </summary>
public class IndexModel : PageModel
{
    /// <summary>docs/specs/014-controls-panel.md FR-024: the same legend as the game screen's.</summary>
    public ControlsPanelViewModel ControlsPanel { get; } = ControlsPanelViewModel.For(isPlaying: false);

    public void OnGet()
    {
    }
}
