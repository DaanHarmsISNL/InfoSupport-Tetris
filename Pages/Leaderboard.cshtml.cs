using infosupport_tetris.Domain;
using infosupport_tetris.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace infosupport_tetris.Pages;

/// <summary>
/// docs/specs/010-view-leaderboard.md: the leaderboard's stable, shareable, bookmarkable address
/// (FR-015, DR-194). Server-rendered and synchronous -- the board is read from an in-memory store
/// within the same request (DR-176, DR-191), so there is no separate network round-trip from the
/// browser's point of view that could leave the reader on an indefinite spinner. Per DR-185's own
/// allowance, this page therefore models only the two outcomes that can actually occur for a
/// synchronous server render: `Ready` (rows, possibly zero of them) and `Empty` (entryCount = 0,
/// FR-018). A genuine `Failed` load -- an unhandled exception while reading the store -- falls
/// through to the application's existing <see cref="ErrorModel"/> page rather than a bespoke
/// retry UI, since there is nothing here that a client-side retry could do differently: the next
/// request re-reads the same in-memory store the same way. `Loading`/`Stale`/10-second-timeout
/// (DR-188, DR-190) are modelled for the one place they *can* occur here -- the show-more action,
/// which this page implements as a link that reloads the page with a larger window size, so growing
/// the window is itself just another synchronous, idempotent read (DR-195) rather than a fetch that
/// can time out or go stale independently of the page it sits on.
/// </summary>
public class LeaderboardModel : PageModel
{
    private const int DefaultWindowSize = 10;
    private const int SecondStepWindowSize = 50;
    private const int StepIncrement = 50;

    private readonly ILeaderboardQueryService _queryService;

    public LeaderboardModel(ILeaderboardQueryService queryService) => _queryService = queryService;

    public LeaderboardView View { get; private set; } = null!;

    /// <summary>DR-174: the window size show-more would grow to next, or null once the whole board is shown.</summary>
    public int? NextWindowSize { get; private set; }

    public void OnGet(int? windowSize)
    {
        var size = NormalizeWindowSize(windowSize);
        View = _queryService.GetView(WindowRequest.Top(size), DateTimeOffset.UtcNow);

        NextWindowSize = View.HasMore ? NextStep(size) : null;
    }

    private static int NormalizeWindowSize(int? requested)
    {
        if (requested is null || requested < DefaultWindowSize)
        {
            return DefaultWindowSize;
        }

        return requested.Value;
    }

    // DR-174: 10 -> 50 -> 100 -> 150 -> ...
    private static int NextStep(int current) =>
        current < SecondStepWindowSize ? SecondStepWindowSize : current + StepIncrement;
}
