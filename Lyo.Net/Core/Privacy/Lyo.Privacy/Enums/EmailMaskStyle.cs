using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Enums;

/// <summary>How email matches are rewritten when <see cref="EmailRedactionRule" /> implements <see cref="IRedactionMatchFormatter" />.</summary>
public enum EmailMaskStyle
{
    /// <summary>Use <see cref="RedactionPolicy.Placeholder" /> for the full match.</summary>
    PolicyPlaceholder,

    /// <summary>Partial local part plus domain, e.g. <c>j***@example.com</c>. Plus-tags: <c>a***+***@b.co</c>.</summary>
    PartialLocalPreserveDomain,

    /// <summary>Mask local and obfuscate host before the public suffix, e.g. <c>j***@***.example.com</c>.</summary>
    PartialLocalMaskDomain
}