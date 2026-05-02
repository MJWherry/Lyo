using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Abstractions;

/// <summary>Sanitised text plus per-kind counts. Does not echo discovered secrets in <see cref="ToString" />.</summary>
/// <param name="InputUtf16Length">UTF-16 code units in the input to the redactor (<c>null</c> when input was <c>null</c>).</param>
/// <param name="OutputUtf16Length">UTF-16 code units in <see cref="Text" /> (<c>null</c> when <see cref="Text" /> is <c>null</c>).</param>
/// <param name="PolicyName">Same policy name supplied to the redactor policy / JSON options, when any.</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed record RedactionResult(
    string? Text,
    IReadOnlyDictionary<RedactionKind, int> CountsByKind,
    int? InputUtf16Length = null,
    int? OutputUtf16Length = null,
    string? PolicyName = null)
{
    public int TotalRuns => CountsByKind.Values.Sum();

    public bool HadRedactions => TotalRuns > 0;

    private string DebuggerDisplay {
        get {
            var sb = new StringBuilder(64);
            sb.Append("Runs=")
                .Append(TotalRuns)
                .Append(", In=")
                .Append(InputUtf16Length?.ToString() ?? "null")
                .Append(", Out=")
                .Append(OutputUtf16Length?.ToString() ?? "null")
                .Append(", Policy=")
                .Append(PolicyName ?? "null");

            if (TotalRuns <= 0)
                return sb.ToString();

            sb.Append(" [");
            var first = true;
            foreach (var kv in CountsByKind) {
                if (kv.Value <= 0)
                    continue;

                if (!first)
                    sb.Append(", ");

                first = false;
                sb.Append(kv.Key).Append(':').Append(kv.Value);
            }

            sb.Append(']');
            return sb.ToString();
        }
    }

    public static RedactionResult Empty(string? original, string? policyName = null)
        => new(original, ImmutableDictionary<RedactionKind, int>.Empty, original is null ? null : original.Length, original is null ? null : original.Length, policyName);

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder(80);
        sb.Append("RedactionResult(Runs=")
            .Append(TotalRuns)
            .Append(", HadRedactions=")
            .Append(HadRedactions)
            .Append(", In=")
            .Append(InputUtf16Length?.ToString() ?? "null")
            .Append(", Out=")
            .Append(OutputUtf16Length?.ToString() ?? "null")
            .Append(", Policy=")
            .Append(PolicyName ?? "null");

        if (TotalRuns > 0) {
            sb.Append(", Kinds=[");
            var first = true;
            foreach (var kv in CountsByKind) {
                if (kv.Value <= 0)
                    continue;

                if (!first)
                    sb.Append(", ");

                first = false;
                sb.Append(kv.Key).Append(':').Append(kv.Value);
            }

            sb.Append(']');
        }

        sb.Append(')');
        return sb.ToString();
    }
}