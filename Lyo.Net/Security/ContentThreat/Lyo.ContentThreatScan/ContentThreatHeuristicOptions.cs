namespace Lyo.ContentThreatScan;

/// <summary>Heuristic engine controls (MIME/extension gate, slicing, binary heuristic, regex weights).</summary>
public sealed class ContentThreatHeuristicOptions
{
    /// <summary>Analyzed prefix length capped for CPU safety.</summary>
    public int MaxBytesToAnalyze { get; set; } = 524_288;

    /// <summary>If true and content looks binary, heuristic returns Clean with no contributions.</summary>
    public bool SkipIfLikelyBinary { get; set; } = true;

    /// <summary>If replacement characters exceed decoded length * ratio, classify as binary.</summary>
    public double NonPrintableLikelyBinaryRatio { get; set; } = 0.12;

    /// <summary>Reject eligibility when null bytes detected in sampled prefix.</summary>
    public bool TreatNullOctetAsBinary { get; set; } = true;

    /// <summary>When MIME and filename are absent, heuristic-scan anyway (noisier; default off).</summary>
    public bool AllowScanWhenHintsMissing { get; set; }

    /// <summary>Lowercase dotted extensions excluding leading dot (e.g. json, txt).</summary>
    public HashSet<string> TextExtensions { get; set; } = new(
        StringComparer.OrdinalIgnoreCase) {
        "txt",
        "log",
        "json",
        "xml",
        "csv",
        "yaml",
        "yml",
        "html",
        "htm",
        "svg",
        "md",
        "sql",
        "cshtml",
        "js",
        "ts",
        "css"
    };

    /// <summary>Case-insensitive content-type prefixes (e.g. text/, application/json).</summary>
    public string[] ContentTypePrefixes { get; set; } = ["text/", "application/json", "application/xml"];

    /// <summary>Additional exact content types (includes application/javascript, etc.).</summary>
    public HashSet<string> ExactContentTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase) {
        "application/javascript",
        "application/x-javascript",
        "application/csv",
        "application/graphql",
        "application/x-www-form-urlencoded"
    };

    /// <summary>Upper bound on cumulative SQL rule points before truncation.</summary>
    public decimal MaxCategoryContributionSqlInjection { get; set; } = 60m;

    /// <summary>Upper bound on cumulative script-ish rule points before truncation.</summary>
    public decimal MaxCategoryContributionScriptInjection { get; set; } = 60m;

    /// <summary>Upper bound on other rule points bucket.</summary>
    public decimal MaxCategoryContributionOther { get; set; } = 30m;
}
