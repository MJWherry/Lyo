using System.Reflection;
using System.Text.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Formatter;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Worker;

/// <summary>
/// Seeds <c>jobrun</c> (properties plus a coerced <c>Parameters</c> map) and re-formats <see cref="FormatterLyoType.IsFormattable"/> values that contain <c>{</c> whenever
/// context grows (CLR strings and formatter editor-kind types). Json, Xml, and collection values stay as-is so JSON braces are not treated as placeholders.
/// </summary>
internal sealed class JobWorkerParameterFormatter
{
    private const int MaxFormatPasses = 8;

    private static readonly BindingFlags PropertyFlags = BindingFlags.Public | BindingFlags.Instance;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, object?> _bag = new(StringComparer.OrdinalIgnoreCase);
    private readonly IFormatterService? _formatter;
    private readonly Dictionary<string, string> _originals = new(StringComparer.OrdinalIgnoreCase);

    public JobWorkerParameterFormatter(IFormatterService? formatter, JobRunRes run)
    {
        ArgumentHelpers.ThrowIfNull(run);
        _formatter = formatter;
        Run = run;
        SnapshotOriginals(run);
        SeedJobRun();
        ReformatStringParameters();
    }

    public JobRunRes Run { get; private set; }

    /// <summary>Adds or replaces a named context object, then re-formats string parameters from their original templates.</summary>
    public void AddContext(string name, object? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        _bag[name] = value;
        ReformatStringParameters();
    }

    /// <summary>Formats <paramref name="template"/> against the current bag. Without a formatter, the template is returned unchanged.</summary>
    public string Format(string template)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template);
        return _formatter is null ? template : _formatter.Format(template, _bag);
    }

    private void SnapshotOriginals(JobRunRes run)
    {
        foreach (var parameter in run.JobRunParameters ?? []) {
            if (!IsFormattableString(parameter) || string.IsNullOrEmpty(parameter.Value) || parameter.Value.IndexOf('{') < 0)
                continue;

            _originals[parameter.Key] = UnwrapJsonString(parameter.Value) ?? parameter.Value;
        }
    }

    private void SeedJobRun()
    {
        var jobrun = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in typeof(JobRunRes).GetProperties(PropertyFlags)) {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;
            if (string.Equals(prop.Name, nameof(JobRunRes.JobRunParameters), StringComparison.Ordinal))
                continue;
            try {
                jobrun[prop.Name] = prop.GetValue(Run);
            }
            catch {
                // Ignore getters that throw.
            }
        }

        jobrun["Parameters"] = BuildParametersMap();
        _bag["jobrun"] = jobrun;
    }

    private Dictionary<string, object?> BuildParametersMap()
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in Run.JobRunParameters ?? [])
            map[parameter.Key] = Coerce(parameter);
        return map;
    }

    private void ReformatStringParameters()
    {
        if (_formatter is null || _originals.Count == 0)
            return;

        for (var pass = 0; pass < MaxFormatPasses; pass++) {
            var parameters = Run.JobRunParameters;
            if (parameters is null || parameters.Count == 0)
                return;

            var list = parameters.ToList();
            var changed = false;
            for (var i = 0; i < list.Count; i++) {
                var parameter = list[i];
                if (!IsFormattableString(parameter))
                    continue;
                if (!_originals.TryGetValue(parameter.Key, out var original))
                    continue;

                string formatted;
                try {
                    formatted = _formatter.Format(original, _bag);
                }
                catch {
                    formatted = original;
                }

                var wrapped = LyoTypeInfo.String.ToJson(formatted);
                if (string.Equals(wrapped, parameter.Value, StringComparison.Ordinal))
                    continue;
                list[i] = parameter with { Value = wrapped };
                changed = true;
            }

            if (!changed)
                return;

            Run = Run with { JobRunParameters = list };
            if (_bag["jobrun"] is Dictionary<string, object?> jobrun)
                jobrun["Parameters"] = BuildParametersMap();
        }
    }

    private static bool IsFormattableString(JobRunParameterRes parameter) => FormatterLyoType.IsFormattableName(parameter.Type);

    private static object? Coerce(JobRunParameterRes parameter)
    {
        var value = parameter.Value;
        if (value is null)
            return null;

        var known = LyoTypeInfo.FromName(parameter.Type);
        if (known != LyoTypeInfo.Unknown) {
            var target = known.IsClr ? known.Type : typeof(string);
            try {
                return JsonSerializer.Deserialize(value, target, JsonOptions);
            }
            catch (JsonException) {
                // Older plaintext values fall through to TypeConversion.
            }
            catch (NotSupportedException) { }

            if (TypeConversion.TryConvertTo(value, known.Type, out var converted) && converted is not null)
                return converted;
        }

        return value;
    }

    private static string? UnwrapJsonString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        try {
            return JsonSerializer.Deserialize<string>(value, JsonOptions);
        }
        catch (JsonException) {
            return value;
        }
    }
}
