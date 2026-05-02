using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Enums;

/// <summary>Which detectors are enabled for <see cref="ApiSecretRedactionRule" />.</summary>
[Flags]
public enum ApiSecretPatterns
{
    None = 0,
    AwsAccessKey = 1 << 0,
    GitHubPersonalAccessToken = 1 << 1,

    /// <summary><c>NAME=value</c> assignments with value length ≥ MinimumAssignmentValueLength.</summary>
    HighEntropyAssignment = 1 << 2
}