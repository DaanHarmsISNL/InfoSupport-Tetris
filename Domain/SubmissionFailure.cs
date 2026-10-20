namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/009-submit-score.md §5.3: why one submission attempt failed, and whether trying
/// again could succeed. Immutable; a later attempt replaces it wholesale rather than mutating it.
/// </summary>
public sealed class SubmissionFailure
{
    public SubmissionFailureReason Reason { get; }

    /// <summary>DR-127: plain language naming the actual problem -- never a raw error or status code.</summary>
    public string Message { get; }

    public bool Retryable { get; }

    public SubmissionFailure(SubmissionFailureReason reason, string message, bool retryable)
    {
        Reason = reason;
        Message = message;
        Retryable = retryable;
    }

    /// <summary>§5.4's Retryable column, so callers never have to restate it alongside a reason.</summary>
    public static SubmissionFailure For(SubmissionFailureReason reason, string message) => reason switch
    {
        SubmissionFailureReason.NameMissing => new SubmissionFailure(reason, message, retryable: true),
        SubmissionFailureReason.NameTooLong => new SubmissionFailure(reason, message, retryable: true),
        SubmissionFailureReason.NameHasForbiddenCharacters => new SubmissionFailure(reason, message, retryable: true),
        SubmissionFailureReason.FiguresRejected => new SubmissionFailure(reason, message, retryable: false),
        SubmissionFailureReason.RunUnavailable => new SubmissionFailure(reason, message, retryable: false),
        SubmissionFailureReason.AlreadySubmitted => new SubmissionFailure(reason, message, retryable: false),
        SubmissionFailureReason.NetworkUnavailable => new SubmissionFailure(reason, message, retryable: true),
        SubmissionFailureReason.Timeout => new SubmissionFailure(reason, message, retryable: true),
        SubmissionFailureReason.ServerError => new SubmissionFailure(reason, message, retryable: true),
        _ => new SubmissionFailure(reason, message, retryable: false)
    };
}
