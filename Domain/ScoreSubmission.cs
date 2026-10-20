namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/009-submit-score.md §5.1: the player's in-progress attempt to submit one finished
/// run. Transient and never persisted -- it lives exactly as long as the game-over state does
/// (Run Availability Window, DR-106). The real client for this feature is the browser (no JS
/// framework exists in this project, so the actual state machine runs in Index.cshtml's script),
/// but this type models the same lifecycle in code so the double-submission and retry rules
/// (DR-115, DR-125, DR-126) are expressible and testable headlessly (NFR-010).
/// </summary>
public sealed class ScoreSubmission
{
    public Guid RunId { get; }

    /// <summary>What is currently in the name field, before normalisation (DR-110). Never stored.</summary>
    public string DraftName { get; private set; }

    public SubmissionStatus Status { get; private set; } = SubmissionStatus.Unsubmitted;

    public int AttemptCount { get; private set; }

    public Guid? EntryId { get; private set; }

    public PlayerName? StoredName { get; private set; }

    public SubmissionFailure? LastFailure { get; private set; }

    public ScoreSubmission(Guid runId, string draftName = "")
    {
        RunId = runId;
        DraftName = draftName;
    }

    public void UpdateDraftName(string draftName)
    {
        if (Status is SubmissionStatus.Submitted or SubmissionStatus.Declined)
        {
            throw new InvalidOperationException($"Cannot edit the name once a submission has reached {Status}.");
        }

        DraftName = draftName.Length > PlayerName.MaxLength ? draftName[..PlayerName.MaxLength] : draftName;
    }

    /// <summary>DR-125: a submit activation while already Submitting is ignored, not queued.</summary>
    public bool TryBeginSubmit()
    {
        if (Status is SubmissionStatus.Submitted or SubmissionStatus.Declined or SubmissionStatus.Submitting)
        {
            return false;
        }

        Status = SubmissionStatus.Submitting;
        AttemptCount++;
        LastFailure = null;
        return true;
    }

    /// <summary>DR-119/DR-126: reached by a fresh create or by DR-115's idempotent return of an existing entry -- indistinguishable here, by design.</summary>
    public void MarkSubmitted(Guid entryId, PlayerName storedName)
    {
        EntryId = entryId;
        StoredName = storedName;
        Status = SubmissionStatus.Submitted;
        LastFailure = null;
    }

    public void MarkFailed(SubmissionFailure failure)
    {
        if (Status is SubmissionStatus.Submitted or SubmissionStatus.Declined)
        {
            throw new InvalidOperationException($"Cannot fail a submission that already reached {Status}.");
        }

        Status = SubmissionStatus.Failed;
        LastFailure = failure;
    }

    /// <summary>DR-116/DR-117: skipping, or playing again without submitting. Terminal; no transition leaves it.</summary>
    public void Decline()
    {
        if (Status == SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Cannot decline a submission that has already succeeded.");
        }

        Status = SubmissionStatus.Declined;
    }
}
