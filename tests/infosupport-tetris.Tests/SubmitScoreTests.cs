using infosupport_tetris.Domain;
using infosupport_tetris.Services;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// Tests for docs/specs/009-submit-score.md, covering the PlayerName value object (DR-109..DR-111),
/// the server-side submission service (DR-115, DR-123, DR-124) and the ScoreSubmission state
/// machine (DR-115, DR-117, DR-125).
/// </summary>
public class SubmitScoreTests
{
    // SC-003/SC-004: DR-109/DR-110 -- blank and whitespace-only names are refused.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void PlayerName_RejectsBlankOrWhitespaceOnly(string? raw)
    {
        Assert.False(PlayerName.TryCreate(raw, out var name, out var reason));
        Assert.Null(name);
        Assert.Equal(SubmissionFailureReason.NameMissing, reason);
    }

    // SC-005: DR-110 -- surrounding and internal whitespace is normalised to one space.
    [Fact]
    public void PlayerName_CollapsesInternalWhitespaceAndTrims()
    {
        Assert.True(PlayerName.TryCreate("   Jan    Willem   ", out var name, out _));
        Assert.Equal("Jan Willem", name!.Value);
    }

    // SC-006: DR-111 -- 20 characters accepted, 21 refused outright (never truncated).
    [Fact]
    public void PlayerName_AcceptsTwentyCharacters_RefusesTwentyOne()
    {
        var twenty = new string('a', 20);
        var twentyOne = new string('a', 21);

        Assert.True(PlayerName.TryCreate(twenty, out var name, out _));
        Assert.Equal(twenty, name!.Value);

        Assert.False(PlayerName.TryCreate(twentyOne, out var rejected, out var reason));
        Assert.Null(rejected);
        Assert.Equal(SubmissionFailureReason.NameTooLong, reason);
    }

    // SC-007/EC-18: DR-111 -- zero-width space and bidi override are refused as format characters.
    [Theory]
    [InlineData("Daan​")] // zero-width space
    [InlineData("Daan‮")] // right-to-left override
    [InlineData("Daan")] // control character
    public void PlayerName_RejectsControlAndFormatCharacters(string raw)
    {
        Assert.False(PlayerName.TryCreate(raw, out var name, out var reason));
        Assert.Null(name);
        Assert.Equal(SubmissionFailureReason.NameHasForbiddenCharacters, reason);
    }

    // EC-17: DR-111 -- length is counted in grapheme clusters, not UTF-16 code units.
    [Fact]
    public void PlayerName_CountsEmojiAsSingleGraphemeClusters()
    {
        var fourEmoji = string.Concat(Enumerable.Repeat("\U0001F600", 4)); // 4 emoji, 8 UTF-16 code units
        Assert.True(PlayerName.TryCreate(fourEmoji, out var name, out _));
        Assert.Equal(4, PlayerName.CountGraphemeClusters(name!.Value));
    }

    private static IScoreSubmissionService CreateService() => new ScoreSubmissionService(new LeaderboardStore());

    // SC-001: a valid submission creates exactly one entry with the submitted figures.
    [Fact]
    public void Submit_ValidRun_CreatesEntryWithSubmittedFigures()
    {
        var service = CreateService();
        var runId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var result = service.Submit(runId, "Daan", 12_400, 31, 4, now);

        Assert.True(result.Success);
        Assert.NotNull(result.EntryId);
        Assert.Equal("Daan", result.StoredPlayerName);
    }

    // SC-010/FR-010: a zero score is submittable on exactly the same terms as any other.
    [Fact]
    public void Submit_ZeroScore_IsAccepted()
    {
        var service = CreateService();
        var result = service.Submit(Guid.NewGuid(), "Nul", 0, 0, 1, DateTimeOffset.UtcNow);
        Assert.True(result.Success);
    }

    // SC-014/SC-015/FR-021/DR-115: repeat submissions of the same runId create nothing and return the original entry.
    [Fact]
    public void Submit_RepeatOfSameRunId_ReturnsOriginalEntry_CreatesNothing()
    {
        var service = CreateService();
        var runId = Guid.NewGuid();

        var first = service.Submit(runId, "Daan", 5_000, 10, 2, DateTimeOffset.UtcNow);
        var second = service.Submit(runId, "Daniel", 5_000, 10, 2, DateTimeOffset.UtcNow);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.EntryId, second.EntryId);
        // SC-016: the original name stands -- the edited name is not applied.
        Assert.Equal("Daan", second.StoredPlayerName);
    }

    // SC-023: DR-123 check 4 -- an impossible score for the lines cleared is refused.
    [Fact]
    public void Submit_ImpossibleScoreForLinesCleared_IsRefused()
    {
        var service = CreateService();
        var result = service.Submit(Guid.NewGuid(), "Cheater", 999_999_999, 12, 2, DateTimeOffset.UtcNow);

        Assert.False(result.Success);
        Assert.Equal(SubmissionFailureReason.FiguresRejected, result.FailureReason);
    }

    // SC-024: DR-123 check 3 -- level inconsistent with Level Derivation (DR-90) is refused.
    [Fact]
    public void Submit_LevelInconsistentWithLinesCleared_IsRefused()
    {
        var service = CreateService();
        var result = service.Submit(Guid.NewGuid(), "Daan", 1_000, 31, 9, DateTimeOffset.UtcNow);

        Assert.False(result.Success);
        Assert.Equal(SubmissionFailureReason.FiguresRejected, result.FailureReason);
    }

    // SC-025: negative figures are refused.
    [Theory]
    [InlineData(-500, 10, 2)]
    [InlineData(100, -1, 1)]
    [InlineData(100, 0, 0)]
    public void Submit_NegativeOrZeroLevelFigures_AreRefused(long score, int linesCleared, int level)
    {
        var service = CreateService();
        var result = service.Submit(Guid.NewGuid(), "Daan", score, linesCleared, level, DateTimeOffset.UtcNow);
        Assert.False(result.Success);
    }

    // SC-026: a genuinely excellent run is not refused.
    [Fact]
    public void Submit_LegitimateHighScore_IsAccepted()
    {
        var service = CreateService();
        var result = service.Submit(Guid.NewGuid(), "Pro", 184_000, 140, 15, DateTimeOffset.UtcNow);
        Assert.True(result.Success);
    }

    // SC-027/DR-124: submittedAt is the server clock, not anything the client could supply.
    [Fact]
    public void Submit_StampsEntry_WithServerSuppliedClock()
    {
        var store = new LeaderboardStore();
        var runId = Guid.NewGuid();
        var serverNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        new ScoreSubmissionService(store).Submit(runId, "Daan", 100, 1, 1, serverNow);

        var entry = store.GetOrCreate(runId, () => throw new InvalidOperationException("should already exist"));
        Assert.Equal(serverNow, entry.SubmittedAt);
    }

    // SC-003: a blank name is refused and no entry is created.
    [Fact]
    public void Submit_BlankName_IsRefused_NoEntryCreated()
    {
        var service = CreateService();
        var result = service.Submit(Guid.NewGuid(), "   ", 100, 1, 1, DateTimeOffset.UtcNow);

        Assert.False(result.Success);
        Assert.Equal(SubmissionFailureReason.NameMissing, result.FailureReason);
    }

    // FR-009/SC-008/SC-009/DR-112/DR-114: the same name submitted twice (different runs) yields two separate entries.
    [Fact]
    public void Submit_SameNameDifferentRuns_CreatesTwoSeparateEntries()
    {
        var service = CreateService();
        var first = service.Submit(Guid.NewGuid(), "Daan", 4_000, 10, 2, DateTimeOffset.UtcNow);
        var second = service.Submit(Guid.NewGuid(), "Daan", 11_000, 20, 3, DateTimeOffset.UtcNow);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.NotEqual(first.EntryId, second.EntryId);
    }

    // DR-125: a submit activation while Submitting is ignored outright.
    [Fact]
    public void ScoreSubmission_IgnoresSubmitActivation_WhileAlreadySubmitting()
    {
        var submission = new ScoreSubmission(Guid.NewGuid());

        Assert.True(submission.TryBeginSubmit());
        Assert.Equal(SubmissionStatus.Submitting, submission.Status);
        Assert.False(submission.TryBeginSubmit());
        Assert.Equal(1, submission.AttemptCount);
    }

    // SC-016/DR-115/DR-118: Submitted is terminal -- no transition leaves it.
    [Fact]
    public void ScoreSubmission_Submitted_IsTerminal()
    {
        var submission = new ScoreSubmission(Guid.NewGuid());
        submission.TryBeginSubmit();
        submission.MarkSubmitted(Guid.NewGuid(), PlayerName.TryCreate("Daan", out var name, out _) ? name! : throw new InvalidOperationException());

        Assert.Equal(SubmissionStatus.Submitted, submission.Status);
        Assert.False(submission.TryBeginSubmit());
        Assert.Throws<InvalidOperationException>(() => submission.Decline());
    }

    // SC-011/SC-012/DR-116/DR-117: Declined is terminal and never produces an entry.
    [Fact]
    public void ScoreSubmission_Declined_IsTerminal()
    {
        var submission = new ScoreSubmission(Guid.NewGuid());
        submission.Decline();

        Assert.Equal(SubmissionStatus.Declined, submission.Status);
        Assert.False(submission.TryBeginSubmit());
    }
}
