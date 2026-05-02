using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Internal;
using Lyo.Privacy.Metrics;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Xml;

/// <summary>Redacts XML by element local name; optionally pipes non-sensitive text through <see cref="ITextRedactor" />.</summary>
public sealed class XmlRedactor
{
    private readonly IMetrics _metrics;
    private readonly XmlRedactorOptions _options;
    private readonly ITextRedactor? _text;

    public XmlRedactor(XmlRedactorOptions options, ITextRedactor? textRedactorForNonSensitive = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
        _text = textRedactorForNonSensitive;
        _metrics = metrics ?? NullMetrics.Instance;
    }

    public RedactionResult RedactXml(string? xml)
    {
        using (_metrics.StartTimer(PrivacyMetricNames.XmlDuration, PrivacyObservation.TagsForPolicy(_options.PolicyName))) {
            _metrics.IncrementCounter(PrivacyMetricNames.XmlOperations, 1, PrivacyObservation.TagsForPolicy(_options.PolicyName));
            if (xml is null)
                return RedactionResult.Empty(null, _options.PolicyName);

            if (xml.Length == 0)
                return RedactionResult.Empty("", _options.PolicyName);

            try {
                var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
                var counts = new Dictionary<RedactionKind, int>();
                if (doc.Root is not null)
                    VisitElement(doc.Root, counts);

                var sb = new StringBuilder(xml.Length + 32);
                using (var xw = XmlWriter.Create(sb, new() { OmitXmlDeclaration = true, Indent = false, Async = false }))
                    doc.WriteTo(xw);

                var output = sb.ToString();
                var result = new RedactionResult(output, counts.ToImmutableDictionary(), xml.Length, output.Length, _options.PolicyName);
                foreach (var kv in result.CountsByKind) {
                    if (kv.Value <= 0)
                        continue;

                    _metrics.IncrementCounter(PrivacyMetricNames.XmlRedactionsByKind, kv.Value, PrivacyObservation.TagsForKind(kv.Key, _options.PolicyName));
                }

                return result;
            }
            catch (XmlException ex) {
                if (_text is null)
                    throw new InvalidOperationException("Invalid XML and no ITextRedactor for fallback.", ex);

                _metrics.IncrementCounter(PrivacyMetricNames.XmlFallbackToText, 1, PrivacyObservation.TagsForPolicy(_options.PolicyName));
                var r = _text.Redact(xml);
                return r with { PolicyName = _options.PolicyName ?? r.PolicyName };
            }
        }
    }

    private void VisitElement(XElement el, IDictionary<RedactionKind, int> counts)
    {
        foreach (var n in el.Nodes().ToList()) {
            if (n is XElement child) {
                VisitElement(child, counts);
                continue;
            }

            if (n is not XText tx)
                continue;

            if (!TryStrategy(el.Name.LocalName, out var strat)) {
                if (_text is not null && _options.ApplyTextRedactorToNonSensitiveText) {
                    var r = _text.Redact(tx.Value);
                    foreach (var kv in r.CountsByKind)
                        counts[kv.Key] = (counts.TryGetValue(kv.Key, out var c) ? c : 0) + kv.Value;

                    tx.Value = r.Text ?? "";
                }

                continue;
            }

            BumpXml(counts);
            if (strat == XmlScalarStrategy.RemoveElement) {
                el.Remove();
                return;
            }

            tx.Value = _options.Placeholder;
        }
    }

    private bool TryStrategy(string localName, out XmlScalarStrategy strat)
    {
        foreach (var kv in _options.SensitiveElementLocalNames) {
            if (!kv.Key.Equals(localName, StringComparison.OrdinalIgnoreCase))
                continue;

            strat = kv.Value;
            return true;
        }

        strat = default;
        return false;
    }

    private static void BumpXml(IDictionary<RedactionKind, int> counts)
    {
        var k = RedactionKind.XmlSensitive;
        counts[k] = (counts.TryGetValue(k, out var c) ? c : 0) + 1;
    }
}