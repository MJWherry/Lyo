using System.Diagnostics;
using System.Text;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Rules;

/// <summary>Controls partial email masking for <see cref="EmailRedactionRule" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EmailMaskOptions
{
    /// <summary>Replace the full address with <see cref="RedactionPolicy.Placeholder" />.</summary>
    public static EmailMaskOptions PolicyPlaceholder { get; } = new() { UsePolicyPlaceholder = true };

    public bool UsePolicyPlaceholder { get; set; }

    /// <summary>How many characters stay visible at the start of the base local part (before a <c>+</c> tag).</summary>
    public int VisibleLocalPrefixLength { get; set; } = 1;

    /// <summary>How many characters stay visible at the end of the base local part (after the middle is masked).</summary>
    public int VisibleLocalSuffixLength { get; set; }

    /// <summary>Literal inserted where the middle of the local part was stripped.</summary>
    public string LocalMaskLiteral { get; set; } = "***";

    /// <summary>When false, the at-sign is swapped for <see cref="AtReplacement" /> (or <see cref="LocalMaskLiteral" /> when that is null/empty).</summary>
    public bool PreserveAtSign { get; set; } = true;

    /// <summary>
    /// When <see cref="PreserveAtSign" /> is false, text between the masked local part and the domain; falls back to
    /// <see cref="LocalMaskLiteral" /> when null or empty.
    /// </summary>
    public string? AtReplacement { get; set; }

    /// <summary>
    /// When true, characters like <c>.</c> or <c>-</c> in the local part stay if they sit between visible prefix/suffix runs; when false, they become
    /// <see cref="SeparatorMaskChar" /> when they border a masked region.
    /// </summary>
    public bool PreserveLocalSeparators { get; set; } = true;

    public char SeparatorMaskChar { get; set; } = '*';

    /// <summary>How many characters to show from the start of the domain host (first label).</summary>
    public int VisibleDomainPrefixLength { get; set; }

    /// <summary>How many characters to show from the end of the domain host (first label), after optional masking in the middle.</summary>
    public int VisibleDomainSuffixLength { get; set; }

    /// <summary>Literal used to mask the middle of the first domain label.</summary>
    public string DomainMaskLiteral { get; set; } = "***";

    /// <summary>Stand-in for the <c>+tag</c> segment when a plus-address is present (after the literal <c>+</c>).</summary>
    public string PlusTagMaskLiteral { get; set; } = "***";

    /// <summary>When true and prefix/suffix lengths are 0, emit <c>@</c> plus the full domain unchanged.</summary>
    public bool PreserveEntireDomainHost { get; set; }

    /// <summary>When true, keep everything from the first dot in the domain (for example <c>.example.com</c>) after the first label is masked.</summary>
    public bool PreserveDomainFromFirstDot { get; set; }

    /// <summary>Keeps the first local characters and the full domain host.</summary>
    public static EmailMaskOptions PartialLocalPreserveDomain(int visibleLocalPrefixLength = 1)
        => new() { VisibleLocalPrefixLength = visibleLocalPrefixLength, PreserveEntireDomainHost = true };

    /// <summary>Masks the first domain label and keeps the rest from the first dot.</summary>
    public static EmailMaskOptions PartialLocalMaskFirstDomainLabel(int visibleLocalPrefixLength = 1)
        => new() { VisibleLocalPrefixLength = visibleLocalPrefixLength, PreserveDomainFromFirstDot = true, VisibleDomainPrefixLength = 0 };

    /// <inheritdoc />
    public override string ToString()
    {
        if (UsePolicyPlaceholder)
            return "EmailMaskOptions { UsePolicyPlaceholder = true }";

        var sb = new StringBuilder(128);
        sb.Append("EmailMaskOptions { VisibleLocalPrefixLength = ")
            .Append(VisibleLocalPrefixLength)
            .Append(", VisibleLocalSuffixLength = ")
            .Append(VisibleLocalSuffixLength)
            .Append(", PreserveAtSign = ")
            .Append(PreserveAtSign)
            .Append(", PreserveEntireDomainHost = ")
            .Append(PreserveEntireDomainHost)
            .Append(", PreserveDomainFromFirstDot = ")
            .Append(PreserveDomainFromFirstDot)
            .Append(", VisibleDomainPrefixLength = ")
            .Append(VisibleDomainPrefixLength)
            .Append(", VisibleDomainSuffixLength = ")
            .Append(VisibleDomainSuffixLength)
            .Append(" }");

        return sb.ToString();
    }

    internal static EmailMaskOptions FromLegacy(EmailMaskStyle style, int visibleLocalPrefixLength)
        => style switch {
            EmailMaskStyle.PolicyPlaceholder => PolicyPlaceholder,
            EmailMaskStyle.PartialLocalPreserveDomain => new() { VisibleLocalPrefixLength = visibleLocalPrefixLength, PreserveEntireDomainHost = true },
            EmailMaskStyle.PartialLocalMaskDomain => new() {
                VisibleLocalPrefixLength = visibleLocalPrefixLength, PreserveDomainFromFirstDot = true, VisibleDomainPrefixLength = 0
            },
            var _ => PolicyPlaceholder
        };
}