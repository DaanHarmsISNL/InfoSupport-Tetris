using System.Globalization;
using System.Text;

namespace infosupport_tetris.Domain;

/// <summary>
/// docs/specs/009-submit-score.md §5.3 (DR-109..DR-111): the normalised display label on a
/// <see cref="LeaderboardEntry"/>. A label, never an identity (DR-112) -- equality is ordinal on
/// <see cref="Value"/> and carries no meaning about who typed it.
/// </summary>
public sealed class PlayerName : IEquatable<PlayerName>
{
    public const int MinLength = 1;
    public const int MaxLength = 20;

    public string Value { get; }

    private PlayerName(string value) => Value = value;

    /// <summary>
    /// DR-110: normalises raw input (NFC, trim, collapse internal whitespace) then validates it
    /// per DR-109/DR-111. Returns the failure reason on rejection rather than throwing, so both
    /// the client-side guard and the server-side authority (FR-002..FR-005) can report *which*
    /// rule failed without parsing an exception message.
    /// </summary>
    public static bool TryCreate(string? raw, out PlayerName? name, out SubmissionFailureReason? failureReason)
    {
        name = null;
        failureReason = null;

        var normalised = Normalise(raw);

        if (normalised.Length == 0)
        {
            failureReason = SubmissionFailureReason.NameMissing;
            return false;
        }

        var graphemeCount = CountGraphemeClusters(normalised);
        if (graphemeCount > MaxLength)
        {
            failureReason = SubmissionFailureReason.NameTooLong;
            return false;
        }

        if (ContainsForbiddenCharacter(normalised))
        {
            failureReason = SubmissionFailureReason.NameHasForbiddenCharacters;
            return false;
        }

        name = new PlayerName(normalised);
        return true;
    }

    /// <summary>DR-110: NFC normalisation, trim, then collapse every internal whitespace run to one space.</summary>
    public static string Normalise(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        var normalised = raw.Normalize(NormalizationForm.FormC).Trim();
        if (normalised.Length == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(normalised.Length);
        var lastWasWhitespace = false;
        foreach (var ch in normalised)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasWhitespace)
                {
                    builder.Append(' ');
                }

                lastWasWhitespace = true;
            }
            else
            {
                builder.Append(ch);
                lastWasWhitespace = false;
            }
        }

        return builder.ToString();
    }

    /// <summary>DR-111: counted in Unicode extended grapheme clusters, not UTF-16 code units.</summary>
    public static int CountGraphemeClusters(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        var count = 0;
        while (enumerator.MoveNext())
        {
            count++;
        }

        return count;
    }

    /// <summary>DR-111: no control characters (C0/C1) and no Unicode format characters (category Cf).</summary>
    private static bool ContainsForbiddenCharacter(string text)
    {
        foreach (var ch in text)
        {
            if (char.IsControl(ch))
            {
                return true;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.Format)
            {
                return true;
            }
        }

        return false;
    }

    public bool Equals(PlayerName? other) => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as PlayerName);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;
}
