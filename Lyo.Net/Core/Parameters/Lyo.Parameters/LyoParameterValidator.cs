using System.Text.RegularExpressions;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;

namespace Lyo.Parameters;

/// <summary>
/// Checks supplied parameter values against their declared specs, and checks the specs themselves at write time. One implementation serves every feature that declares
/// parameters, so a job run and a report generation reject the same input for the same reason with the same wording.
/// </summary>
/// <remarks>
/// <para>
/// Every method collects errors instead of throwing, so a caller can report all problems at once. Callers decide what an error becomes: a problem-details response, a validation
/// exception, or a UI message.
/// </para>
/// </remarks>
public static class LyoParameterValidator
{
    /// <summary>Upper bound on a definition-supplied regex, matching the database column limit. Longer patterns are rejected instead of compiled.</summary>
    public const int MaxValidationRegexLength = 500;

    /// <summary>Match timeout that guards against catastrophic backtracking in definition-supplied patterns.</summary>
    public static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Validates supplied values against the declared parameters. Returns an empty list when everything passes, so the result can be tested with <c>Count == 0</c> instead of
    /// a null check.
    /// </summary>
    /// <param name="specs">Parameters declared on the definition. An empty list means only <paramref name="rejectUnknownKeys" /> can produce an error.</param>
    /// <param name="values">Values supplied by the caller. Repeated keys are all checked.</param>
    /// <param name="rejectUnknownKeys">When true, keys the definition never declared are an error instead of being ignored.</param>
    public static IReadOnlyList<string> Validate(
        IReadOnlyList<LyoParameterSpec> specs,
        IReadOnlyList<LyoParameterValueSpec> values,
        bool rejectUnknownKeys = false)
    {
        var errors = new List<string>();
        var byKey = values.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        if (rejectUnknownKeys) {
            var known = new HashSet<string>(specs.Select(d => d.Key), StringComparer.OrdinalIgnoreCase);
            var unknown = byKey.Keys.Where(k => !known.Contains(k)).OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
            if (unknown.Count > 0)
                errors.Add($"Unknown parameter key(s) not declared on the definition: {string.Join(", ", unknown)}.");
        }

        foreach (var spec in specs) {
            byKey.TryGetValue(spec.Key, out var provided);

            // Ciphertext satisfies "required": encrypted-only parameters have no plaintext value.
            if (spec.Required && (provided is null || provided.Count == 0 || provided.All(p => string.IsNullOrEmpty(p.Value) && !p.HasEncryptedValue))) {
                errors.Add($"Parameter '{spec.Key}' is required.");
                continue;
            }

            if (provided is null || provided.Count == 0)
                continue;

            foreach (var supplied in provided)
                ValidateValue(spec, supplied.Value, errors);
        }

        return errors;
    }

    /// <summary>
    /// Validates a declared parameter itself: key and type present, regex compilable and within the length cap, lengths non-negative and ordered. Call at create/update time so
    /// bad metadata fails on write instead of on every later run.
    /// </summary>
    /// <param name="spec">Parameter as declared.</param>
    /// <param name="errors">List collecting error messages.</param>
    public static void ValidateSpec(LyoParameterSpec spec, List<string> errors)
    {
        var label = string.IsNullOrWhiteSpace(spec.Key) ? "(no key)" : spec.Key;
        if (string.IsNullOrWhiteSpace(spec.Key))
            errors.Add("Parameter Key is required.");

        if (string.IsNullOrWhiteSpace(spec.Type))
            errors.Add($"Parameter '{label}' Type is required.");

        if (!string.IsNullOrEmpty(spec.ValidationRegex)) {
            if (spec.ValidationRegex!.Length > MaxValidationRegexLength)
                errors.Add($"Parameter '{label}' ValidationRegex exceeds {MaxValidationRegexLength} characters.");
            else {
                try {
                    _ = new Regex(spec.ValidationRegex, RegexOptions.None, RegexMatchTimeout);
                }
                catch (ArgumentException) {
                    errors.Add($"Parameter '{label}' ValidationRegex is not a valid regular expression.");
                }
            }
        }

        if (spec.MinLength is < 0)
            errors.Add($"Parameter '{label}' MinLength must not be negative.");

        if (spec.MaxLength is < 0)
            errors.Add($"Parameter '{label}' MaxLength must not be negative.");

        if (spec is { MinLength: { } min, MaxLength: { } max } && min > max)
            errors.Add($"Parameter '{label}' MinLength ({min}) must not exceed MaxLength ({max}).");

        // A half-set default channel is the failure mode this split introduces, so catch it on write instead of on the first run that needs the default.
        switch (spec.DefaultKind) {
            case LyoParameterDefaultKind.Expression when string.IsNullOrWhiteSpace(spec.DefaultTemplate):
                errors.Add($"Parameter '{label}' uses an expression default but DefaultTemplate is empty.");
                break;
            case LyoParameterDefaultKind.Literal when !string.IsNullOrWhiteSpace(spec.DefaultTemplate):
                errors.Add($"Parameter '{label}' has a DefaultTemplate but DefaultKind is Literal; switch it to Expression or clear the template.");
                break;
        }
    }

    /// <summary>Reports keys declared more than once on the same definition, which would make value lookup ambiguous.</summary>
    /// <param name="specs">Parameters declared on the definition.</param>
    /// <param name="errors">List collecting error messages.</param>
    public static void ValidateUniqueKeys(IEnumerable<LyoParameterSpec> specs, List<string> errors)
    {
        var duplicates = specs.Where(p => !string.IsNullOrWhiteSpace(p.Key)).GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var key in duplicates)
            errors.Add($"Parameter key '{key}' appears more than once; parameter keys must be unique per definition.");
    }

    /// <summary>Applies length, pattern, allowed-value, and type checks to one supplied value. Empty values are treated as unset and skipped.</summary>
    /// <param name="spec">Parameter as declared.</param>
    /// <param name="rawValue">Value exactly as supplied, before any JSON unwrapping.</param>
    /// <param name="errors">List collecting error messages.</param>
    private static void ValidateValue(LyoParameterSpec spec, string? rawValue, List<string> errors)
    {
        // Optional dropdowns post "" when nothing is selected. Treat that as unset, not as a failed constraint.
        var value = rawValue ?? string.Empty;
        if (string.IsNullOrEmpty(value))
            return;

        // Length and pattern checks read the string a user typed, not its JSON encoding, so a quoted string is unwrapped first.
        var checkValue = LyoParameterValueJson.Unwrap(spec.Type, value);

        if (spec.MinLength.HasValue && checkValue.Length < spec.MinLength.Value)
            errors.Add($"Parameter '{spec.Key}' must be at least {spec.MinLength} characters.");

        if (spec.MaxLength.HasValue && checkValue.Length > spec.MaxLength.Value)
            errors.Add($"Parameter '{spec.Key}' must not exceed {spec.MaxLength} characters.");

        if (!string.IsNullOrEmpty(spec.ValidationRegex))
            ValidateAgainstRegex(spec.Key, spec.ValidationRegex!, checkValue, errors);

        if (!string.IsNullOrEmpty(spec.AllowedValues) && LyoTypeInfo.FromName(spec.Type).EditorKind != LyoTypeEditorKind.Formatter) {
            var allowed = ParameterListJson.Parse(spec.AllowedValues!);
            if (!allowed.Contains(checkValue, StringComparer.OrdinalIgnoreCase) && !allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
                errors.Add($"Parameter '{spec.Key}' value '{value}' is not one of the allowed values: {string.Join(", ", allowed)}.");
        }

        if (!LyoParameterValueJson.IsAssignable(spec.Type, value))
            errors.Add($"Parameter '{spec.Key}' value '{value}' is not a valid {spec.Type}.");
    }

    /// <summary>Runs a definition-supplied pattern under a length cap and a match timeout, turning every failure mode into an error message instead of an exception.</summary>
    /// <param name="key">Parameter name, for the message.</param>
    /// <param name="pattern">Pattern as declared.</param>
    /// <param name="value">Value to match.</param>
    /// <param name="errors">List collecting error messages.</param>
    private static void ValidateAgainstRegex(string key, string pattern, string value, List<string> errors)
    {
        if (pattern.Length > MaxValidationRegexLength) {
            errors.Add($"Parameter '{key}' has a validation pattern exceeding {MaxValidationRegexLength} characters.");
            return;
        }

        try {
            if (!Regex.IsMatch(value, pattern, RegexOptions.None, RegexMatchTimeout))
                errors.Add($"Parameter '{key}' does not match the required pattern.");
        }
        catch (RegexMatchTimeoutException) {
            errors.Add($"Parameter '{key}' validation pattern timed out.");
        }
        catch (ArgumentException) {
            errors.Add($"Parameter '{key}' has an invalid validation pattern.");
        }
    }

}
