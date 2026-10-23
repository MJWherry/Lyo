using System.Text.RegularExpressions;
using DynamicExpresso;

namespace Lyo.Algorithms;

/// <summary>
/// Allowlist and identifier scan shared by the compiler and the algorithm editors. The compiler rejects anything outside these lists. Identifiers are bare names
/// (<c>Atk</c>), not dotted paths.
/// </summary>
public static class AlgorithmExpressionHelp
{
    private static readonly HashSet<string> Builtins = new(StringComparer.Ordinal) { "Math", "Convert", "DateTime", "DateTimeOffset", "TimeSpan", "true", "false", "null" };

    private static readonly Regex Name = new(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.CultureInvariant);

    /// <summary><c>Math</c> methods an expression may call.</summary>
    public static IReadOnlyList<string> MathMethods { get; } = ["Abs", "Min", "Max", "Round", "Floor", "Ceiling", "Pow", "Sqrt", "Sign"];

    /// <summary><c>Convert</c> methods an expression may call.</summary>
    public static IReadOnlyList<string> ConvertMethods { get; } = ["ToDouble", "ToDecimal", "ToInt32", "ToInt64"];

    /// <summary><c>DateTime</c> and <c>DateTimeOffset</c> members. <c>UtcDateTime</c> exists only on <c>DateTimeOffset</c>.</summary>
    public static IReadOnlyList<string> DateMembers { get; } = ["Year", "Month", "Day", "Hour", "Minute", "Second", "Date", "UtcDateTime"];

    /// <summary><c>DateTime</c> and <c>DateTimeOffset</c> methods.</summary>
    public static IReadOnlyList<string> DateMethods { get; } = ["AddDays", "AddMonths", "AddYears", "AddHours"];

    /// <summary><c>TimeSpan</c> members.</summary>
    public static IReadOnlyList<string> TimeSpanMembers { get; } = ["TotalDays", "TotalHours", "Days"];

    /// <summary>Members allowed on arrays and other non-string collections. An index is also allowed.</summary>
    public static IReadOnlyList<string> CollectionMembers { get; } = ["Length", "Count"];

    /// <summary>Bare identifiers in <paramref name="source" />, in appearance order. <c>Algo</c> and <c>score</c> keys are not identifiers.</summary>
    public static IReadOnlyList<string> Identifiers(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return [];

        try {
            var sites = new List<AlgorithmCallSite>();
            var rewritten = AlgorithmCalls.Placeholders(source, static _ => typeof(double), sites);
            var reserved = new HashSet<string>(sites.Select(static site => site.Name), StringComparer.Ordinal);
            var info = CreateInterpreter().DetectIdentifiers(rewritten);
            return Collect(info.UnknownIdentifiers, reserved);
        }
        catch (Exception) {
            return Collect(Name.Matches(source).Cast<Match>().Select(static match => match.Value), new HashSet<string>(StringComparer.Ordinal));
        }
    }

    private static Interpreter CreateInterpreter()
        => new Interpreter(InterpreterOptions.Default).Reference(typeof(Math)).Reference(typeof(Convert)).Reference(typeof(DateTime)).Reference(typeof(DateTimeOffset)).Reference(typeof(TimeSpan));

    private static IReadOnlyList<string> Collect(IEnumerable<string> candidates, HashSet<string> reserved)
    {
        var names = new List<string>();
        foreach (var name in candidates) {
            if (Builtins.Contains(name) || reserved.Contains(name) || !AlgorithmContextBag.IsIdentifier(name) || names.Contains(name))
                continue;

            names.Add(name);
        }

        return names;
    }

    /// <summary>
    /// Caret completions for <paramref name="prefix" />. Includes bag identifiers, the compiler allowlist, and <c>Algo</c> / <c>score</c> calls. An empty prefix returns nothing.
    /// Members that are not on the allowlist, such as <c>Math.Atan</c>, are never offered.
    /// </summary>
    public static IReadOnlyList<string> Suggestions(string? prefix, IEnumerable<string>? identifiers, IEnumerable<string>? catalogKeys)
    {
        var token = prefix ?? string.Empty;
        if (token.Length == 0)
            return [];

        var items = new List<string>();
        if (identifiers is not null) {
            foreach (var name in identifiers) {
                if (!AlgorithmContextBag.IsIdentifier(name) || Builtins.Contains(name) || items.Contains(name))
                    continue;

                if (Matches(name, token))
                    items.Add(name);
            }
        }

        foreach (var item in AllowlistTokens()) {
            if (Matches(item, token) && !items.Contains(item))
                items.Add(item);
        }

        if (catalogKeys is null)
            return items;

        foreach (var key in catalogKeys) {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var algo = "Algo(\"" + key + "\")";
            var score = "score(\"" + key + "\")";
            if (MatchesCall(algo, key, token))
                items.Add(algo);

            if (MatchesCall(score, key, token))
                items.Add(score);
        }

        return items;
    }

    private static IEnumerable<string> AllowlistTokens()
    {
        foreach (var name in MathMethods)
            yield return "Math." + name;

        foreach (var name in ConvertMethods)
            yield return "Convert." + name;

        foreach (var name in DateMembers)
            yield return "." + name;

        foreach (var name in DateMethods)
            yield return "." + name + "(";

        foreach (var name in TimeSpanMembers)
            yield return "." + name;

        foreach (var name in CollectionMembers)
            yield return "." + name;
    }

    private static bool Matches(string suggestion, string prefix)
        => suggestion.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Tail(suggestion).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesCall(string suggestion, string key, string prefix)
        => suggestion.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static string Tail(string suggestion)
    {
        var start = suggestion.LastIndexOf('.') + 1;
        var end = start;
        while (end < suggestion.Length && (char.IsLetterOrDigit(suggestion[end]) || suggestion[end] == '_'))
            end++;

        return end > start ? suggestion.Substring(start, end - start) : suggestion;
    }
}
