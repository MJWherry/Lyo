using System.Text;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>Detects email-shaped tokens; masking is driven by <see cref="EmailMaskOptions" />.</summary>
public sealed class EmailRedactionRule : IRedactionRule, IRedactionMatchFormatter
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public EmailMaskOptions Options { get; }

    public EmailRedactionRule(EmailMaskOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        Options = options;
    }

    /// <summary>Compatibility ctor mapping <see cref="EmailMaskStyle" /> to <see cref="EmailMaskOptions" />.</summary>
    public EmailRedactionRule(EmailMaskStyle maskStyle = EmailMaskStyle.PolicyPlaceholder, int visibleLocalPrefixLength = 1)
        : this(EmailMaskOptions.FromLegacy(maskStyle, visibleLocalPrefixLength))
        => ArgumentHelpers.ThrowIfLessThan(visibleLocalPrefixLength, 0);

    /// <inheritdoc />
    public string? FormatReplacement(string input, RedactionSpan span)
    {
        if (Options.UsePolicyPlaceholder)
            return null;

        var value = input.Substring(span.Start, span.Length);
        var at = value.IndexOf('@');
        if (at <= 0)
            return null;

        var local = value[..at];
        var domain = value[(at + 1)..];
        var plus = local.IndexOf('+');
        string maskedLocal;
        if (plus >= 0) {
            var baseLocal = local[..plus];
            var tag = local[(plus + 1)..];
            maskedLocal = MaskBaseLocal(baseLocal, Options);
            maskedLocal += tag.Length > 0 ? "+" + Options.PlusTagMaskLiteral : "+";
        }
        else
            maskedLocal = MaskBaseLocal(local, Options);

        var join = Options.PreserveAtSign ? "@" : string.IsNullOrEmpty(Options.AtReplacement) ? Options.LocalMaskLiteral : Options.AtReplacement!;
        var host = MaskDomainHost(domain, Options);
        return maskedLocal + join + host;
    }

    public RedactionKind Kind => RedactionKind.Email;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (Match m in EmailRegex.Matches(input)) {
            if (!m.Success)
                continue;

            yield return new(m.Index, m.Length, Kind);
        }
    }

    private static string MaskBaseLocal(string baseLocal, EmailMaskOptions o)
    {
        if (baseLocal.Length == 0)
            return o.LocalMaskLiteral;

        var pl = Math.Min(o.VisibleLocalPrefixLength, baseLocal.Length);
        var sl = Math.Min(o.VisibleLocalSuffixLength, baseLocal.Length);
        var maxSuffix = Math.Max(0, baseLocal.Length - pl);
        sl = Math.Min(sl, maxSuffix);
        if (pl + sl >= baseLocal.Length) {
            if (baseLocal.Length == 1 && sl == 0 && pl > 0)
                return baseLocal + o.LocalMaskLiteral;

            return baseLocal;
        }

        var midEnd = baseLocal.Length - sl;
        var sb = new StringBuilder(baseLocal.Length + o.LocalMaskLiteral.Length);
        sb.Append(baseLocal[..pl]);
        if (pl < midEnd && pl < baseLocal.Length && IsLocalSep(baseLocal[pl]))
            sb.Append(o.PreserveLocalSeparators ? baseLocal[pl] : o.SeparatorMaskChar);

        sb.Append(o.LocalMaskLiteral);
        if (midEnd > pl && midEnd <= baseLocal.Length && IsLocalSep(baseLocal[midEnd - 1]))
            sb.Append(o.PreserveLocalSeparators ? baseLocal[midEnd - 1] : o.SeparatorMaskChar);

        sb.Append(baseLocal[^sl..]);
        return sb.ToString();
    }

    private static bool IsLocalSep(char c) => c is '.' or '-' or '_';

    private static string MaskDomainHost(string domain, EmailMaskOptions o)
    {
        if (o.PreserveEntireDomainHost)
            return domain;

        var dot = domain.IndexOf('.');
        var firstLabel = dot < 0 ? domain : domain[..dot];
        var restFromDot = dot < 0 ? "" : domain[dot..];
        if (firstLabel.Length == 0)
            return o.DomainMaskLiteral + restFromDot;

        var fp = Math.Min(o.VisibleDomainPrefixLength, firstLabel.Length);
        var maxSuf = Math.Max(0, firstLabel.Length - fp);
        var fs = Math.Min(o.VisibleDomainSuffixLength, maxSuf);
        if (fp + fs >= firstLabel.Length)
            return firstLabel + restFromDot;

        var midEnd = firstLabel.Length - fs;
        var sb = new StringBuilder(firstLabel.Length + o.DomainMaskLiteral.Length);
        sb.Append(firstLabel[..fp]);
        if (fp < midEnd && fp < firstLabel.Length && IsLocalSep(firstLabel[fp]))
            sb.Append(o.PreserveLocalSeparators ? firstLabel[fp] : o.SeparatorMaskChar);

        sb.Append(o.DomainMaskLiteral);
        if (midEnd > fp && midEnd <= firstLabel.Length && IsLocalSep(firstLabel[midEnd - 1]))
            sb.Append(o.PreserveLocalSeparators ? firstLabel[midEnd - 1] : o.SeparatorMaskChar);

        sb.Append(firstLabel[^fs..]);
        sb.Append(restFromDot);
        return sb.ToString();
    }
}