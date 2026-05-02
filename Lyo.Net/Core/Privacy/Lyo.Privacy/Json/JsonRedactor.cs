using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Internal;
using Lyo.Privacy.Metrics;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Json;

/// <summary>Rewrites JSON with scalar redaction for configured keys.</summary>
public sealed class JsonRedactor : IStructuredRedactor
{
    private readonly IMetrics _metrics;
    private readonly JsonRedactorOptions _options;
    private readonly ITextRedactor? _textRedactor;

    public JsonRedactor(JsonRedactorOptions options, ITextRedactor? textRedactorForStrings = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
        if (options.ApplyTextRulesToAllStringValues && textRedactorForStrings is null)
            throw new ArgumentException("ITextRedactor is required when JsonRedactorOptions.ApplyTextRulesToAllStringValues is true.", nameof(textRedactorForStrings));

        _textRedactor = textRedactorForStrings;
        _metrics = metrics ?? NullMetrics.Instance;
    }

    public RedactionResult RedactJson(string? json)
    {
        using (_metrics.StartTimer(PrivacyMetricNames.JsonDuration, PrivacyObservation.TagsForPolicy(_options.PolicyName))) {
            _metrics.IncrementCounter(PrivacyMetricNames.JsonOperations, 1, PrivacyObservation.TagsForPolicy(_options.PolicyName));
            if (json is null)
                return RedactionResult.Empty(null, _options.PolicyName);

            if (string.IsNullOrWhiteSpace(json))
                return RedactionResult.Empty(json, _options.PolicyName);

            return RedactJsonUtf8Core(Encoding.UTF8.GetBytes(json), json.Length);
        }
    }

    /// <summary>Parses UTF-8 JSON without an intermediate <see cref="string" /> for the document parse. <see cref="RedactionResult.InputUtf16Length" /> is null.</summary>
    public RedactionResult RedactJsonUtf8(ReadOnlyMemory<byte> utf8Json)
    {
        using (_metrics.StartTimer(PrivacyMetricNames.JsonDuration, PrivacyObservation.TagsForPolicy(_options.PolicyName))) {
            _metrics.IncrementCounter(PrivacyMetricNames.JsonOperations, 1, PrivacyObservation.TagsForPolicy(_options.PolicyName));
            return utf8Json.Length == 0 ? RedactionResult.Empty(string.Empty, _options.PolicyName) : RedactJsonUtf8Core(utf8Json, null);
        }
    }

    /// <summary>Buffers the input stream then redacts via <see cref="RedactJsonUtf8" />; writes UTF-8 output to <paramref name="utf8Output" />.</summary>
    public void RedactJsonStream(Stream utf8Input, Stream utf8Output)
    {
        using var buf = new MemoryStream();
        utf8Input.CopyTo(buf);
        var res = RedactJsonUtf8(new(buf.GetBuffer(), 0, (int)buf.Length));
        var outBytes = Encoding.UTF8.GetBytes(res.Text ?? string.Empty);
        utf8Output.Write(outBytes, 0, outBytes.Length);
    }

    private RedactionResult RedactJsonUtf8Core(ReadOnlyMemory<byte> utf8Json, int? inputUtf16LengthHint)
    {
        try {
#if NETSTANDARD2_0
            var utf8Arr = utf8Json.ToArray();
            using var doc = JsonDocument.Parse(utf8Arr);
#else
            using var doc = JsonDocument.Parse(utf8Json);
#endif
            var counts = new Dictionary<RedactionKind, int>();
            var capacity = utf8Json.Length <= 64 ? 512 : Math.Min(checked(utf8Json.Length * 2), 16_777_216);
            using var ms = new MemoryStream(capacity);
            using (var w = new Utf8JsonWriter(ms, new() { Indented = true }))
                WriteElement(doc.RootElement, w, counts);

            var output = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            var result = new RedactionResult(output, counts.ToImmutableDictionary(), inputUtf16LengthHint, output.Length, _options.PolicyName);
            PrivacyMetricsRecorder.RecordJsonRedactionResult(_metrics, result, _options.PolicyName);
            return result;
        }
        catch (JsonException ex) {
            _metrics.IncrementCounter(PrivacyMetricNames.JsonFallbackToText, 1, PrivacyObservation.TagsForPolicy(_options.PolicyName));
#if NETSTANDARD2_0
            var json = Encoding.UTF8.GetString(utf8Json.ToArray());
#else
            var json = Encoding.UTF8.GetString(utf8Json.Span);
#endif
            if (_textRedactor is null)
                throw new InvalidOperationException("Cannot redact invalid JSON without an ITextRedactor for fallback.", ex);

            var r = _textRedactor.Redact(json);
            return r with { InputUtf16Length = inputUtf16LengthHint ?? json.Length, PolicyName = _options.PolicyName ?? r.PolicyName };
        }
    }

    private void WriteElement(JsonElement el, Utf8JsonWriter w, IDictionary<RedactionKind, int> counts)
    {
        switch (el.ValueKind) {
            case JsonValueKind.Object:
                w.WriteStartObject();
                foreach (var p in el.EnumerateObject()) {
                    if (TryGetStrategy(p.Name, out var strat) && strat == JsonKeyRedactionStrategy.Remove) {
                        Bump(counts);
                        continue;
                    }

                    w.WritePropertyName(p.Name);
                    if (TryGetStrategy(p.Name, out strat))
                        WriteRedactedForKey(p.Value, w, strat, p.Name, counts);
                    else
                        WriteElementUnchecked(p.Value, w, counts);
                }

                w.WriteEndObject();
                break;
            case JsonValueKind.Array:
                w.WriteStartArray();
                foreach (var item in el.EnumerateArray())
                    WriteElementUnchecked(item, w, counts);

                w.WriteEndArray();
                break;
            default:
                WriteElementUnchecked(el, w, counts);
                break;
        }
    }

    private void WriteElementUnchecked(JsonElement el, Utf8JsonWriter w, IDictionary<RedactionKind, int> counts)
    {
        switch (el.ValueKind) {
            case JsonValueKind.Object:
            case JsonValueKind.Array:
                WriteElement(el, w, counts);
                break;
            case JsonValueKind.String:
                var s = el.GetString();
                if (_options.ApplyTextRulesToAllStringValues && _textRedactor is not null && s is not null) {
                    var r = _textRedactor.Redact(s);
                    foreach (var kv in r.CountsByKind)
                        counts[kv.Key] = (counts.TryGetValue(kv.Key, out var c) ? c : 0) + kv.Value;

                    w.WriteStringValue(r.Text);
                }
                else
                    el.WriteTo(w);

                break;
            default:
                el.WriteTo(w);
                break;
        }
    }

    private bool TryGetStrategy(string name, out JsonKeyRedactionStrategy strategy)
    {
        foreach (var kv in _options.SensitiveKeys) {
            if (!kv.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            strategy = kv.Value;
            return true;
        }

        strategy = default;
        return false;
    }

    private void WriteRedactedForKey(JsonElement el, Utf8JsonWriter w, JsonKeyRedactionStrategy strat, string keyName, IDictionary<RedactionKind, int> counts)
    {
        Bump(counts);
        if (el.ValueKind is JsonValueKind.Object or JsonValueKind.Array) {
            w.WriteStringValue(strat == JsonKeyRedactionStrategy.HashStable 
                ? StableHash(keyName, el) 
                : _options.Placeholder);
            return;
        }

        switch (strat) {
            case JsonKeyRedactionStrategy.HashStable:
                w.WriteStringValue(StableHash(keyName, el));
                break;
            default:
                w.WriteStringValue(_options.Placeholder);
                break;
        }
    }

    private static void Bump(IDictionary<RedactionKind, int> counts)
    {
        var k = RedactionKind.JsonKey;
        counts[k] = (counts.TryGetValue(k, out var c) ? c : 0) + 1;
    }

    private string StableHash(string keyName, JsonElement value)
    {
        var raw = value.GetRawText();
        var salt = _options.StableHashSalt ?? Array.Empty<byte>();
        var payload = Encoding.UTF8.GetBytes(keyName + "\0" + raw);
        using var sha = SHA256.Create();
        var len = salt.Length + payload.Length;
        var buf = new byte[len];
        Buffer.BlockCopy(salt, 0, buf, 0, salt.Length);
        Buffer.BlockCopy(payload, 0, buf, salt.Length, payload.Length);
        var h = sha.ComputeHash(buf);
        return HexPrefix8(h);
    }

    private static string HexPrefix8(byte[] h)
    {
        var n = Math.Min(8, h.Length);
        var chars = new char[2 * n];
        for (var i = 0; i < n; i++) {
            var b = h[i];
            chars[2 * i] = NibbleHex(b >> 4);
            chars[2 * i + 1] = NibbleHex(b & 0xF);
        }

        return new(chars);

        static char NibbleHex(int v)
        {
            v &= 0xF;
            return (char)(v < 10 ? '0' + v : 'A' + (v - 10));
        }
    }
}