using System.Diagnostics;
using Lyo.Common.Enums;

namespace Lyo.Compression.Models;

/// <summary>Configuration for <see cref="Policy.CompressionPolicyAlgorithmSelector" />. Bind from <c>CompressionOptions:Policy</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class CompressionPolicyOptions
{
    /// <summary>Recommended configuration section suffix under <see cref="CompressionServiceOptions.SectionName" />.</summary>
    public const string PolicySectionName = "Policy";

    /// <summary>Skip compression when payload is smaller than this (bytes). Default 4096.</summary>
    public long MinCompressSizeBytes { get; set; } = 4096;

    /// <summary>When <see langword="true" />, apply built-in category/size heuristics before the configured default algorithm.</summary>
    public bool BuiltInDefaultsEnabled { get; set; } = true;

    /// <summary>Default codec when no rule matches (e.g. <c>Brotli</c>). Falls back to <see cref="CompressionServiceOptions.DefaultAlgorithm" /> when unset.</summary>
    public string? DefaultAlgorithm { get; set; }

    /// <summary>Ordered rules; first match wins.</summary>
    public List<CompressionPolicyRuleOptions> Rules { get; set; } = [];

    public override string ToString()
        => $"CompressionPolicyOptions: MinCompressSizeBytes={MinCompressSizeBytes}, BuiltInDefaultsEnabled={BuiltInDefaultsEnabled}, Rules={Rules.Count}";
}

/// <summary>A single policy rule evaluated against <see cref="CompressionSelectionContext" />.</summary>
public sealed class CompressionPolicyRuleOptions
{
    public string? Name { get; set; }

    public List<string> Tenants { get; set; } = [];

    public List<string> ContentTypes { get; set; } = [];

    public List<string> ContentTypePrefixes { get; set; } = [];

    public List<FileTypeCategory> Categories { get; set; } = [];

    public long? MinSizeBytes { get; set; }

    public long? MaxSizeBytes { get; set; }

    /// <summary>When <see langword="false" />, skip compression for matching inputs.</summary>
    public bool? Compress { get; set; }

    /// <summary>Algorithm name (e.g. <c>LZ4</c>, <c>GZip</c>) resolved via <see cref="CompressionAlgorithm.TryFromName" />.</summary>
    public string? Algorithm { get; set; }
}