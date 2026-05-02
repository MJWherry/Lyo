using System.Text.RegularExpressions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Internal;

namespace Lyo.Privacy.Rules;

/// <summary>Heuristic API keys, PATs, and <c>KEY=value</c> material; optional entropy gate.</summary>
public sealed class ApiSecretRedactionRule : IRedactionRule
{
    private static readonly Regex AwsKey = new(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    
    private static readonly Regex GitHubPat = new(@"\bgh[psuro]_[A-Za-z0-9_]{36,255}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Assignment = new(
        @"(?<![A-Za-z0-9_])([A-Za-z][A-Za-z0-9_]{0,63})\s*=\s*([A-Za-z0-9+/=_\-]{16,2048})", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ApiSecretPatterns Patterns { get; }

    public double MinEntropyBitsPerChar { get; }

    public int MinimumAssignmentValueLength { get; }

    public ApiSecretRedactionRule(ApiSecretPatterns patterns, double minEntropyBitsPerChar = 0, int minimumAssignmentValueLength = 16)
    {
        if (patterns == ApiSecretPatterns.None)
            throw new ArgumentException("Select at least one pattern.", nameof(patterns));

        if (minEntropyBitsPerChar < 0)
            throw new ArgumentOutOfRangeException(nameof(minEntropyBitsPerChar));

        if (minimumAssignmentValueLength < 8)
            throw new ArgumentOutOfRangeException(nameof(minimumAssignmentValueLength));

        Patterns = patterns;
        MinEntropyBitsPerChar = minEntropyBitsPerChar;
        MinimumAssignmentValueLength = minimumAssignmentValueLength;
    }

    public RedactionKind Kind => RedactionKind.ApiSecret;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        if ((Patterns & ApiSecretPatterns.AwsAccessKey) != 0) {
            foreach (Match m in AwsKey.Matches(input)) {
                if (m.Success && PassesEntropy(m.Value))
                    yield return new(m.Index, m.Length, Kind);
            }
        }

        if ((Patterns & ApiSecretPatterns.GitHubPersonalAccessToken) != 0) {
            foreach (Match m in GitHubPat.Matches(input)) {
                if (m.Success && PassesEntropy(m.Value))
                    yield return new(m.Index, m.Length, Kind);
            }
        }

        if ((Patterns & ApiSecretPatterns.HighEntropyAssignment) != 0) {
            foreach (Match m in Assignment.Matches(input)) {
                if (!m.Success)
                    continue;

                var valG = m.Groups[2].Value;
                if (valG.Length < MinimumAssignmentValueLength)
                    continue;

                if (!PassesEntropy(valG))
                    continue;

                yield return new(m.Index, m.Length, Kind);
            }
        }
    }

    private bool PassesEntropy(string secret)
    {
        if (MinEntropyBitsPerChar <= 0)
            return true;

        return EntropyEstimator.ShannonBitsPerChar(secret) >= MinEntropyBitsPerChar;
    }
}