using Lyo.Common.Metadata.Records;

namespace Lyo.ContentThreatScan;

/// <summary>Heuristic knobs: MIME/extension gate, slice size, binary check, and regex weights.</summary>
public sealed class ContentThreatHeuristicOptions
{
    /// <summary>Prefix length examined, capped so CPU stays bounded.</summary>
    public int MaxBytesToAnalyze { get; set; } = 524_288;

    /// <summary>When true and the sample looks binary, the heuristic reports Clean and adds no hits.</summary>
    public bool SkipIfLikelyBinary { get; set; } = true;

    /// <summary>If replacement chars exceed decoded length times this ratio, treat the sample as binary.</summary>
    public double NonPrintableLikelyBinaryRatio { get; set; } = 0.12;

    /// <summary>Refuse eligibility when the sampled prefix contains null bytes.</summary>
    public bool TreatNullOctetAsBinary { get; set; } = true;

    /// <summary>Scan anyway when MIME and filename are both missing (noisier; off by default).</summary>
    public bool AllowScanWhenHintsMissing { get; set; }

    /// <summary>Lowercase dotted extensions without the leading dot (e.g. json, txt).</summary>
    public HashSet<string> TextExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase) {
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

    /// <summary>Extra exact content types (includes application/javascript and similar).</summary>
    public HashSet<string> ExactContentTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase) {
        FileTypeInfo.JavaScript.MimeType,
        FileTypeInfo.Csv.MimeType,
        FileTypeInfo.Graphql.MimeType,
        FileTypeInfo.WwwFormUrlEncoded.MimeType
    };

    /// <summary>Cap on accumulated SQL-rule points before further hits are truncated.</summary>
    public decimal MaxCategoryContributionSqlInjection { get; set; } = 60m;

    /// <summary>Cap on accumulated script-ish rule points before truncation.</summary>
    public decimal MaxCategoryContributionScriptInjection { get; set; } = 60m;

    /// <summary>Cap on the remaining rule-points bucket.</summary>
    public decimal MaxCategoryContributionOther { get; set; } = 30m;
}