using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Lyo.FeatureFlags;

/// <summary>Rebinds feature flags when <see cref="IConfiguration.GetReloadToken" /> fires. A bad document leaves the last good snapshot.</summary>
internal sealed class FeatureFlagConfigurationReload : IDisposable
{
    private readonly ILogger<FeatureFlagConfigurationReload>? _logger;
    private readonly string _sectionName;
    private readonly IDisposable _subscription;
    private readonly IFeatureFlagUpdater _updater;
    private readonly IConfiguration _configuration;

    public FeatureFlagConfigurationReload(IConfiguration configuration, string sectionName, IFeatureFlagUpdater updater, ILogger<FeatureFlagConfigurationReload>? logger)
    {
        _configuration = configuration;
        _sectionName = sectionName;
        _updater = updater;
        _logger = logger;
        _subscription = ChangeToken.OnChange(configuration.GetReloadToken, Reload);
    }

    public void Dispose() => _subscription.Dispose();

    private void Reload()
    {
        try
        {
            var options = FeatureFlagConfiguration.Bind(_configuration, _sectionName);
            if (!_updater.TryApply(options))
                _logger?.LogWarning("Feature flag reload for section {Section} was rejected. The previous flags stay in effect.", _sectionName);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Feature flag reload for section {Section} failed. The previous flags stay in effect.", _sectionName);
        }
    }
}
