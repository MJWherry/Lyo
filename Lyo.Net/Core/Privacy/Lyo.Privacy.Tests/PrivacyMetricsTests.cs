using Lyo.Metrics;
using Lyo.Metrics.Models;
using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Json;
using Lyo.Privacy.Metrics;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;
using Lyo.Privacy.Xml;

namespace Lyo.Privacy.Tests;

public sealed class PrivacyMetricsTests
{
    [Fact]
    public void TextRedactor_records_operations_and_redactions_by_kind()
    {
        var m = new CapturingMetrics();
        var r = new TextRedactor(new RedactionPolicyBuilder().AddRule(new EmailRedactionRule()).Build(), m);
        _ = r.Redact("x a@b.co y");
        Assert.Contains(m.Increments, x => x.Name == PrivacyMetricNames.TextOperations);
        Assert.Contains(m.Increments, x => x.Name == PrivacyMetricNames.TextRedactionsByKind && x.TagKey == "kind" && x.TagVal == nameof(RedactionKind.Email));
    }

    [Fact]
    public void JsonRedactor_records_fallback_metric()
    {
        var m = new CapturingMetrics();
        var text = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule())), m);
        var j = new JsonRedactor(new(), text, m);
        _ = j.RedactJson("not json a@b.co");
        Assert.Contains(m.Increments, x => x.Name == PrivacyMetricNames.JsonFallbackToText);
    }

    [Fact]
    public void XmlRedactor_records_operations()
    {
        var m = new CapturingMetrics();
        var x = new XmlRedactor(new() { PolicyName = "t" }, null, m);
        _ = x.RedactXml("<r><password>x</password></r>");
        Assert.Contains(m.Increments, y => y.Name == PrivacyMetricNames.XmlOperations);
        Assert.Contains(m.Increments, y => y is { Name: PrivacyMetricNames.XmlRedactionsByKind, TagKey: "kind" } && y.TagVal == nameof(RedactionKind.XmlSensitive));
    }

    private sealed class CapturingMetrics : IMetrics
    {
        public readonly List<(string Name, IConvertible? Value, string? TagKey, string? TagVal)> Increments = new();

        public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
        {
            string? tk = null, tv = null;
            if (tags is not null) {
                foreach (var t in tags) {
                    tk = t.Item1;
                    tv = t.Item2;
                    break;
                }
            }

            Increments.Add((name, value, tk, tv));
        }

        public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) { }

        public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) { }

        public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) { }

        public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null) { }

        public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null) => default;

        public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null) { }

        public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) { }
    }
}