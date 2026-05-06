using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lyo.Common.Extensions;
using Lyo.ContentThreatScan.Abstractions;
using Lyo.Exceptions;

namespace Lyo.ContentThreatScan;

/// <summary>Regex-weighted heuristic engine with per-category spend caps.</summary>
public sealed class DefaultContentThreatScanner : IContentThreatScanner
{
    private sealed class RuleDefinition(string ruleId, string patternProbe, ContentThreatCategory category, decimal points, string regexPattern)
    {
        public string RuleId { get; } = ruleId;

        public string PatternProbe { get; } = patternProbe;

        public ContentThreatCategory Category { get; } = category;

        public decimal Points { get; } = points;

        public string RegexPattern { get; } = regexPattern;
    }

    private readonly ContentThreatHeuristicOptions _heuristic;

    private static readonly RuleDefinition[] Rules = BuildDefinitions();
    private static readonly ConcurrentDictionary<string, Regex> Compiled = new(StringComparer.Ordinal);

    public DefaultContentThreatScanner(ContentThreatHeuristicOptions heuristic)
    {
        ArgumentHelpers.ThrowIfNull(heuristic);
        _heuristic = heuristic;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentThreatContribution>> CollectHeuristicContributionsAsync(
        ReadOnlyMemory<byte> sampledBytes,
        ContentThreatScanContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        if (!LooksTextEligible(_heuristic, context))
            return Task.FromResult<IReadOnlyList<ContentThreatContribution>>(Array.Empty<ContentThreatContribution>());

        var data = sampledBytes.ToArray();
        cancellationToken.ThrowIfCancellationRequested();

        if (_heuristic.SkipIfLikelyBinary && LooksBinary(data, _heuristic))
            return Task.FromResult<IReadOnlyList<ContentThreatContribution>>(Array.Empty<ContentThreatContribution>());

        var textScan = ProbeTextForPatterns(data);

        decimal spentSql = 0m;
        decimal spentScript = 0m;
        decimal spentOther = 0m;
        var hits = new List<ContentThreatContribution>();

        foreach (var rule in Rules) {
            cancellationToken.ThrowIfCancellationRequested();

            try {
                if (rule.PatternProbe.Length != 0 && CultureInfo.InvariantCulture.CompareInfo.IndexOf(textScan, rule.PatternProbe, CompareOptions.OrdinalIgnoreCase) < 0)
                    continue;

                Regex rx = CachedRegex(rule.RegexPattern);
                if (!rx.IsMatch(textScan))
                    continue;

                var cap = CategoryCap(rule.Category);
                var spent = SpendFor(rule.Category, spentSql, spentScript, spentOther);
                var room = cap - spent;
                if (room <= 0m)
                    continue;

                var applied = room < rule.Points ? room : rule.Points;
                if (applied <= 0m)
                    continue;

                AccumulateSpend(rule.Category, ref spentSql, ref spentScript, ref spentOther, applied);
                hits.Add(new(rule.RuleId, rule.Category, applied, $"{(rule.PatternProbe.Length == 0 ? "regex" : "substr")}:{rule.PatternProbe}"));
            }
            catch (RegexMatchTimeoutException) {
               
            }
        }

        return Task.FromResult<IReadOnlyList<ContentThreatContribution>>(hits);
    }

    static Regex CachedRegex(string pattern) =>
        Compiled.GetOrAdd(
            pattern,
            p =>
#if NETSTANDARD2_0
                new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)
#else
                new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(80))
#endif
        );

    decimal CategoryCap(ContentThreatCategory category) =>
        category switch {
            ContentThreatCategory.SqlInjection => _heuristic.MaxCategoryContributionSqlInjection,
            ContentThreatCategory.ScriptInjection => _heuristic.MaxCategoryContributionScriptInjection,
            _ => _heuristic.MaxCategoryContributionOther
        };

    static decimal SpendFor(ContentThreatCategory category, decimal sql, decimal script, decimal other) =>
        category switch {
            ContentThreatCategory.SqlInjection => sql,
            ContentThreatCategory.ScriptInjection => script,
            _ => other
        };

    static void AccumulateSpend(ContentThreatCategory category, ref decimal sql, ref decimal script, ref decimal other, decimal applied)
    {
        switch (category) {
            case ContentThreatCategory.SqlInjection:
                sql += applied;
                break;
            case ContentThreatCategory.ScriptInjection:
                script += applied;
                break;
            default:
                other += applied;
                break;
        }
    }

    private static bool LooksBinary(byte[] data, ContentThreatHeuristicOptions options)
    {
        if (data.Length == 0)
            return false;

        if (options.TreatNullOctetAsBinary) {
            for (var i = 0; i < data.Length; i++) {
                if (data[i] == 0)
                    return true;
            }
        }

        var weird = 0;
        foreach (var b in data) {
            if (b == 9 || b == 10 || b == 13)
                continue;
            if (b < 32 || b == 127)
                weird++;
        }

        if (weird / (double)data.Length >= options.NonPrintableLikelyBinaryRatio)
            return true;

        return ReplacementCharRatio(data) >= options.NonPrintableLikelyBinaryRatio;
    }

    static double ReplacementCharRatio(byte[] data)
    {
        var decoded = Encoding.UTF8.GetString(data);
        var bad = 0;
        foreach (var ch in decoded) {
            if (ch == '\uFFFD')
                bad++;
        }

        return bad / (double)Math.Max(decoded.Length, 1);
    }

    private static string ProbeTextForPatterns(ReadOnlySpan<byte> data)
    {
        try {
            var s = Encoding.UTF8.GetString(data.ToArray());
            const int probeMax = 1_048_576;
            return s.Length <= probeMax ? s : s[..probeMax];
        }
        catch {
            return string.Empty;
        }
    }

    private static bool LooksTextEligible(ContentThreatHeuristicOptions heur, ContentThreatScanContext ctx)
    {
        if (!string.IsNullOrWhiteSpace(ctx.ContentType)) {
            var trimmedCt = ctx.ContentType!.Trim();
            foreach (var prefix in heur.ContentTypePrefixes) {
                if (trimmedCt.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            var coreMime = MimeCore(trimmedCt);
            if (coreMime.Length > 0 && heur.ExactContentTypes.Contains(coreMime))
                return true;
        }

        var ext = ExtensionDots(ctx.OriginalFileName);
        if (ext.Length > 0 && heur.TextExtensions.Contains(ext))
            return true;

        return heur.AllowScanWhenHintsMissing
            && string.IsNullOrWhiteSpace(ctx.ContentType)
            && string.IsNullOrWhiteSpace(ctx.OriginalFileName);
    }

    static string MimeCore(string trimmedContentType)
    {
        var semi = trimmedContentType.IndexOf(';');
        return (semi >= 0 ? trimmedContentType[..semi] : trimmedContentType).Trim().ToLowerInvariant();
    }

    private static string ExtensionDots(string? fileName)
    {
        if (fileName.IsNullOrWhitespace())
            return string.Empty;

        fileName = Path.GetFileName(fileName.Trim());
        var dot = fileName.LastIndexOf('.');
        if (dot <= 0 || dot == fileName.Length - 1)
            return string.Empty;

        var ext = fileName[(dot + 1)..];
        ext = CultureInfo.InvariantCulture.TextInfo.ToLower(ext);
        return ext;
    }

    private static RuleDefinition[] BuildDefinitions() => [
        Rule("sql.union_select", "union", ContentThreatCategory.SqlInjection, 35m, @"\bunion\s+select\b"),
            Rule("sql.drop_table", "drop", ContentThreatCategory.SqlInjection, 40m, @";\s*drop\s+table\b"),
            Rule("sql.delete_from", ";delete", ContentThreatCategory.SqlInjection, 38m, @";\s*delete\s+from\b"),
            Rule("sql.or_tautology", "'", ContentThreatCategory.SqlInjection, 18m, @"'[^']*'\s*(or|=)\s*(1\s*=\s*1|'1')"),
            Rule("sql.benchmark", "benchmark", ContentThreatCategory.SqlInjection, 32m, @"\bbenchmark\s*\("),
            Rule("sql.sleep", "sleep(", ContentThreatCategory.SqlInjection, 32m, @"\bsleep\s*\(\s*[0-9]"),
            Rule("sql.xp_cmdshell", "xp_cmdshell", ContentThreatCategory.SqlInjection, 60m, @"\bxp_cmdshell\b"),
            Rule("sql.into_outfile", "into outfile", ContentThreatCategory.SqlInjection, 45m, @"\binto\s+(dumpfile|outfile)\b"),
            Rule("sql.waitfor_delay", "waitfor delay", ContentThreatCategory.SqlInjection, 35m, @"\bwaitfor\s+delay\b"),
            Rule("script.tag", "<script", ContentThreatCategory.ScriptInjection, 30m, @"<\s*script\b"),

            Rule("script.javascript_scheme", "javascript:", ContentThreatCategory.ScriptInjection, 28m, @"javascript\s*:"),
            Rule("script.vbscript_scheme", "vbscript:", ContentThreatCategory.ScriptInjection, 28m, @"vbscript\s*:"),

            Rule("script.event_handler_inline", "=", ContentThreatCategory.ScriptInjection, 15m,
                @"\bon(?:load|click|mouseover|mouseout|dblclick|error|focus)\s*="),

            Rule("script.asp_template", "<%@", ContentThreatCategory.ScriptInjection, 22m, @"<%@\s*"),
            Rule("script.eval_paren", "eval(", ContentThreatCategory.ScriptInjection, 24m, @"\beval\s*\("),

            Rule("script.powershell", "invoke-", ContentThreatCategory.Other, 22m,
                @"\binvoke-(expression|command)\b"),

            Rule("script.base64_exe", "", ContentThreatCategory.Other, 18m,
                @"frombase64string\s*\(")
    ];

    private static RuleDefinition Rule(string id, string probe, ContentThreatCategory cat, decimal pts, string rx) =>
        new(id, probe, cat, pts, rx);
}
