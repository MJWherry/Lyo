using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Enums;

/// <summary>How email matches are rewritten when <see cref="EmailRedactionRule" /> implements <see cref="IRedactionMatchFormatter" />.</summary>
public enum EmailMaskStyle
{
    /// <summary>Replace the full match with <see cref="RedactionPolicy.Placeholder" />.</summary>
    PolicyPlaceholder,

    /// <summary>Partial local part plus domain, for example <c>j***@example.com</c>. Plus-tags: <c>a***+***@b.co</c>.</summary>
    PartialLocalPreserveDomain,

    /// <summary>Mask the local part and obfuscate the host before the public suffix, for example <c>j***@***.example.com</c>.</summary>
    PartialLocalMaskDomain
}