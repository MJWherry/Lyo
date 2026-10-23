namespace Lyo.FeatureFlags;

/// <summary>
/// Replaces the flags <see cref="IFeatureFlagService" /> and <see cref="Microsoft.Extensions.Options.IOptionsMonitor{FeatureFlagOptions}" /> share.
/// <see cref="Microsoft.Extensions.Options.IOptions{FeatureFlagOptions}" /> stays the document from registration. A Config host calls this. <c>AddFeatureFlagsFromConfiguration</c>
/// calls it when configuration reloads.
/// </summary>
public interface IFeatureFlagUpdater
{
    /// <summary>
    /// Validates <paramref name="options" /> and swaps the snapshot when it is valid. Returns false and keeps the previous snapshot when validation fails. A null argument
    /// throws. The new document replaces the whole set. A name that is gone becomes unknown, which is off.
    /// </summary>
    bool TryApply(FeatureFlagOptions options);
}