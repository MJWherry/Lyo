using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Json;
using Lyo.Privacy.Text;
using Lyo.Privacy.Xml;

namespace Lyo.Privacy.Metrics;

/// <summary>
/// Metric names written by <see cref="TextRedactor" />, <see cref="JsonRedactor" />, and <see cref="XmlRedactor" /> when an Lyo.Metrics <c>IMetrics</c> implementation is
/// supplied. Counters that break out <see cref="RedactionKind" /> use tag <c>kind</c>. When a policy name is configured, tag <c>policy</c> is added.
/// </summary>
public static class PrivacyMetricNames
{
    /// <summary>Counter: incremented once per text redaction call.</summary>
    public const string TextOperations = "lyo.privacy.text.operations";

    /// <summary>Timing: wall-clock time for each text redaction call.</summary>
    public const string TextDuration = "lyo.privacy.text.duration";

    /// <summary>Counter: total redaction runs (merged regions) in the produced output.</summary>
    public const string TextRedactionRuns = "lyo.privacy.text.redaction_runs";

    /// <summary>Counter: redactions grouped by <see cref="RedactionKind" />; tags <c>kind</c> and optionally <c>policy</c>.</summary>
    public const string TextRedactionsByKind = "lyo.privacy.text.redactions.by_kind";

    /// <summary>Counter: incremented once per JSON redaction call.</summary>
    public const string JsonOperations = "lyo.privacy.json.operations";

    /// <summary>Timing: wall-clock time for each JSON redaction call.</summary>
    public const string JsonDuration = "lyo.privacy.json.duration";

    /// <summary>Counter: JSON key-based redactions (scalar or object replacement).</summary>
    public const string JsonKeyRedactions = "lyo.privacy.json.key_redactions";

    /// <summary>Counter: JSON redactions by kind (includes nested text-rule counts when those run on string values).</summary>
    public const string JsonRedactionsByKind = "lyo.privacy.json.redactions.by_kind";

    /// <summary>Counter: invalid JSON triggered fallback to <see cref="ITextRedactor" />.</summary>
    public const string JsonFallbackToText = "lyo.privacy.json.fallback_to_text";

    /// <summary>Counter: incremented once per XML redaction call.</summary>
    public const string XmlOperations = "lyo.privacy.xml.operations";

    /// <summary>Timing: wall-clock time for each XML redaction call.</summary>
    public const string XmlDuration = "lyo.privacy.xml.duration";

    /// <summary>Counter: XML redactions grouped by kind.</summary>
    public const string XmlRedactionsByKind = "lyo.privacy.xml.redactions.by_kind";

    /// <summary>Counter: invalid XML triggered fallback to <see cref="ITextRedactor" />.</summary>
    public const string XmlFallbackToText = "lyo.privacy.xml.fallback_to_text";
}