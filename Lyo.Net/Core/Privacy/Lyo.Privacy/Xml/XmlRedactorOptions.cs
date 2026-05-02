using System.Diagnostics;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Xml;

/// <summary>Options for <see cref="XmlRedactor" /> (case-insensitive element local names).</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class XmlRedactorOptions
{
    public IReadOnlyDictionary<string, XmlScalarStrategy> SensitiveElementLocalNames { get; set; } =
        new Dictionary<string, XmlScalarStrategy>(StringComparer.OrdinalIgnoreCase) { ["password"] = XmlScalarStrategy.Placeholder };

    public string Placeholder { get; set; } = "[redacted]";

    /// <summary>Runs <see cref="ITextRedactor" /> on text nodes whose element is not listed in <see cref="SensitiveElementLocalNames" />.</summary>
    public bool ApplyTextRedactorToNonSensitiveText { get; set; }

    public string? PolicyName { get; set; }

    private string DebuggerDisplay
        => $"XmlRedactorOptions(policy={PolicyName ?? "∅"}, sensitiveKeys={SensitiveElementLocalNames.Count}, textPass={ApplyTextRedactorToNonSensitiveText})";
}