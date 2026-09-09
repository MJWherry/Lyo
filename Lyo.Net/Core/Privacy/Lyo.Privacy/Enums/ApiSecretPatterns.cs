using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Enums;

/// <summary>Which detectors <see cref="ApiSecretRedactionRule" /> enables.</summary>
[Flags]
public enum ApiSecretPatterns
{
    None = 0,
    AwsAccessKey = 1 << 0,
    GitHubPersonalAccessToken = 1 << 1,

    /// <summary><c>NAME=value</c> assignments whose value length is ≥ MinimumAssignmentValueLength.</summary>
    HighEntropyAssignment = 1 << 2,

    /// <summary>
    /// Format-B Lyo opaque tokens such as <c>lyo_pat_live_01HXY...</c>. Matched independently of <see cref="HighEntropyAssignment" /> so they
    /// redact even when they are not assigned to a variable.
    /// </summary>
    LyoToken = 1 << 3
}