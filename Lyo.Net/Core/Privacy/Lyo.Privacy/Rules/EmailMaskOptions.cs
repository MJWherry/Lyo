using System.Diagnostics;
using System.Text;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Rules;

/// <summary>Configures partial email masking for <see cref="EmailRedactionRule" />.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class EmailMaskOptions
{
    /// <summary>Use <see cref="RedactionPolicy.Placeholder" /> for the full address.</summary>
    public static EmailMaskOptions PolicyPlaceholder { get; } = new() { UsePolicyPlaceholder = true };

    public bool UsePolicyPlaceholder { get; set; }

    /// <summary>Characters kept at the start of the base local part (before <c>+</c> tag).</summary>
    public int VisibleLocalPrefixLength { get; set; } = 1;

    /// <summary>Characters kept at the end of the base local part (after masking the middle).</summary>
    public int VisibleLocalSuffixLength { get; set; }

    /// <summary>Text inserted where the middle of the local part was removed.</summary>
    public string LocalMaskLiteral { get; set; } = "***";

    /// <summary>When false, the at-sign is replaced by <see cref="AtReplacement" /> (or <see cref="LocalMaskLiteral" /> when that is null/empty).</summary>
    public bool PreserveAtSign { get; set; } = true;

    /// <summary>When <see cref="PreserveAtSign" /> is false, text between masked local and domain; defaults to <see cref="LocalMaskLiteral" /> when null or empty.</summary>
    public string? AtReplacement { get; set; }

    /// <summary>
    /// When true, characters like <c>.</c> or <c>-</c> in the local part are kept if they lie between visible prefix/suffix runs; when false, they are replaced by
    /// <see cref="SeparatorMaskChar" /> when bordering a masked region.
    /// </summary>
    public bool PreserveLocalSeparators { get; set; } = true;

    public char SeparatorMaskChar { get; set; } = '*';

    /// <summary>Characters to show from the start of the domain host (first label).</summary>
    public int VisibleDomainPrefixLength { get; set; }

    /// <summary>Characters to show from the end of the domain host (first label), after optional masking in the middle.</summary>
    public int VisibleDomainSuffixLength { get; set; }

    /// <summary>Mask literal for the middle of the first domain label.</summary>
    public string DomainMaskLiteral { get; set; } = "***";

    /// <summary>Replacement for the <c>+tag</c> segment when a plus-address is present (after the literal <c>+</c>).</summary>
    public string PlusTagMaskLiteral { get; set; } = "***";

    /// <summary>When true and prefix/suffix lengths are 0, output <c>@</c> plus full domain unchanged.</summary>
    public bool PreserveEntireDomainHost { get; set; }

    /// <summary>When true, append everything from the first dot in the domain (e.g. <c>.example.com</c>) unchanged after the first label is masked.</summary>
    public bool PreserveDomainFromFirstDot { get; set; }

    private string DebuggerDisplay
        => UsePolicyPlaceholder
            ? "EmailMaskOptions(Placeholder)"
            : $"EmailMaskOptions(local↑{VisibleLocalPrefixLength}/↓{VisibleLocalSuffixLength}, @={PreserveAtSign}, domain↑{VisibleDomainPrefixLength}/↓{VisibleDomainSuffixLength}, host={PreserveEntireDomainHost}, fromDot={PreserveDomainFromFirstDot})";

    /// <summary>First local characters + full domain host.</summary>
    public static EmailMaskOptions PartialLocalPreserveDomain(int visibleLocalPrefixLength = 1)
        => new() { VisibleLocalPrefixLength = visibleLocalPrefixLength, PreserveEntireDomainHost = true };

    /// <summary>Mask first domain label; keep from first dot.</summary>
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