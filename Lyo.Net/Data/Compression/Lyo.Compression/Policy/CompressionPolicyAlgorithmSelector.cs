using Lyo.Common.Enums;
using Lyo.Common.Extensions;
using Lyo.Compression.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Compression.Policy;

/// <summary>Default <see cref="ICompressionAlgorithmSelector" /> driven by <see cref="CompressionPolicyOptions" /> and built-in heuristics.</summary>
public sealed class CompressionPolicyAlgorithmSelector : ICompressionAlgorithmSelector
{
    private readonly CompressionPolicyOptions _policy;
    private readonly CompressionServiceOptions _serviceOptions;
    private readonly ILogger<CompressionPolicyAlgorithmSelector> _logger;

    public CompressionPolicyAlgorithmSelector(
        CompressionPolicyOptions policy,
        CompressionServiceOptions serviceOptions,
        ILogger<CompressionPolicyAlgorithmSelector>? logger = null)
    {
        _policy = policy;
        _serviceOptions = serviceOptions;
        _logger = logger ?? NullLogger<CompressionPolicyAlgorithmSelector>.Instance;
    }

    /// <inheritdoc />
    public CompressionSelectionResult ResolveForCompress(CompressionSelectionContext context)
    {
        ArgumentHelpers.ThrowIfNull(context);

        foreach (var rule in _policy.Rules) {
            if (!RuleMatches(rule, context))
                continue;

            if (rule.Compress == false) {
                _logger.LogDebug("Compression policy rule '{RuleName}' skipped compression", rule.Name ?? "(unnamed)");
                return new(false, null);
            }

            var algo = ResolveAlgorithmName(rule.Algorithm);
            if (algo == null)
                continue;

            _logger.LogDebug("Compression policy rule '{RuleName}' selected {Algorithm}", rule.Name ?? "(unnamed)", algo.Name);
            return new(true, algo);
        }

        if (_policy.BuiltInDefaultsEnabled) {
            var builtIn = ResolveBuiltIn(context);
            if (builtIn != null)
                return builtIn;
        }

        return new(true, ResolveDefaultAlgorithm());
    }

    private CompressionSelectionResult? ResolveBuiltIn(CompressionSelectionContext context)
    {
        if (context.ByteLength < _policy.MinCompressSizeBytes) {
            _logger.LogDebug("Skipping compression: size {Size} below MinCompressSizeBytes {Min}", context.ByteLength, _policy.MinCompressSizeBytes);
            return new(false, null);
        }

        var category = context.FileType.Category;
        if (category is FileTypeCategory.Compressed or FileTypeCategory.Images or FileTypeCategory.Audio) {
            _logger.LogDebug("Skipping compression: category {Category}", category);
            return new(false, null);
        }

        return null;
    }

    private CompressionAlgorithm ResolveDefaultAlgorithm()
        => ResolveAlgorithmName(_policy.DefaultAlgorithm) ?? _serviceOptions.DefaultAlgorithm;

    private static CompressionAlgorithm? ResolveAlgorithmName(string? name)
        => CompressionAlgorithm.TryFromName(name?.Trim());

    private static bool RuleMatches(CompressionPolicyRuleOptions rule, CompressionSelectionContext context)
    {
        if (rule.Tenants.Count > 0) {
            if (context.TenantId.IsNullOrEmpty() || !rule.Tenants.Any(t => t.Equals(context.TenantId, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (rule.MinSizeBytes is { } min && context.ByteLength < min)
            return false;

        if (rule.MaxSizeBytes is { } max && context.ByteLength > max)
            return false;

        if (rule.Categories.Count > 0 && !rule.Categories.Contains(context.FileType.Category))
            return false;

        var contentType = context.ContentType?.Trim();
        if (rule.ContentTypes.Count > 0) {
            if (contentType.IsNullOrEmpty() || !rule.ContentTypes.Any(ct => ct.Equals(contentType, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (rule.ContentTypePrefixes.Count > 0) {
            if (contentType.IsNullOrEmpty() || !rule.ContentTypePrefixes.Any(p => contentType.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }
}
