using infosupport_tetris.Domain;

namespace infosupport_tetris.Services;

/// <summary>
/// docs/specs/009-submit-score.md: the server-side authority for accepting a submission. Performs
/// DR-110/DR-111 name validation and DR-123 figure validation, then asks <see cref="ILeaderboardStore"/>
/// to create the entry idempotently per DR-115. The client's own validation (FR-002..FR-005) is
/// only ever a convenience -- this is the one that actually enforces the rules.
/// </summary>
public interface IScoreSubmissionService
{
    ScoreSubmissionResult Submit(Guid runId, string? rawPlayerName, long score, int linesCleared, int level, DateTimeOffset serverNow);
}

public sealed record ScoreSubmissionResult(bool Success, Guid? EntryId, string? StoredPlayerName, SubmissionFailureReason? FailureReason, string? FailureMessage);

public sealed class ScoreSubmissionService : IScoreSubmissionService
{
    private readonly ILeaderboardStore _store;

    public ScoreSubmissionService(ILeaderboardStore store) => _store = store;

    public ScoreSubmissionResult Submit(Guid runId, string? rawPlayerName, long score, int linesCleared, int level, DateTimeOffset serverNow)
    {
        if (!PlayerName.TryCreate(rawPlayerName, out var playerName, out var nameFailureReason))
        {
            var message = nameFailureReason switch
            {
                SubmissionFailureReason.NameMissing => "A name is required.",
                SubmissionFailureReason.NameTooLong => "Names can be at most 20 characters.",
                SubmissionFailureReason.NameHasForbiddenCharacters => "That name contains a character that is not allowed.",
                _ => "That name is not valid."
            };

            return new ScoreSubmissionResult(false, null, null, nameFailureReason, message);
        }

        if (!AreFiguresValid(score, linesCleared, level))
        {
            return new ScoreSubmissionResult(
                false, null, null, SubmissionFailureReason.FiguresRejected, "The submission was refused.");
        }

        // DR-115: atomic per runId -- a repeat submission (double-click, retry after a lost
        // response, or a genuine race, SC-013..SC-015) creates nothing and returns the original.
        var entry = _store.GetOrCreate(runId, () => new LeaderboardEntry(
            entryId: Guid.NewGuid(),
            playerName: playerName!,
            score: score,
            linesCleared: linesCleared,
            level: level,
            runId: runId,
            submittedAt: serverNow)); // DR-124: server clock, UTC, ignoring any client-supplied time.

        return new ScoreSubmissionResult(true, entry.EntryId, entry.PlayerName.Value, null, null);
    }

    /// <summary>DR-123: the four proportionate sanity checks -- reject the impossible, never the merely impressive.</summary>
    private static bool AreFiguresValid(long score, int linesCleared, int level)
    {
        if (score < 0 || linesCleared < 0 || level < 1)
        {
            return false;
        }

        if (linesCleared > 9_999 || level > 15)
        {
            return false;
        }

        var expectedLevel = Math.Min(linesCleared / 10 + 1, 15);
        if (level != expectedLevel)
        {
            return false;
        }

        var ceiling = 3_200L * linesCleared + 10_000L;
        return score <= ceiling;
    }
}
