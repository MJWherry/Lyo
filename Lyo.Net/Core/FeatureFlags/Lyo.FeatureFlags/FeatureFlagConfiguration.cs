using Lyo.Exceptions.Models;
using Microsoft.Extensions.Configuration;

namespace Lyo.FeatureFlags;

internal static class FeatureFlagConfiguration
{
    public static FeatureFlagOptions Bind(IConfiguration configuration, string sectionName)
    {
        var options = new FeatureFlagOptions();
        foreach (var child in configuration.GetSection(sectionName).GetSection("Flags").GetChildren()) {
            var definition = new FeatureFlagDefinition();
            try {
                child.Bind(definition, binder => binder.ErrorOnUnknownConfiguration = true);
            }
            catch (Exception ex) {
                throw new ConfigurationException($"Flag '{child.Key}' could not be bound.", child.Key, ex);
            }

            options.Flags[child.Key] = definition;
        }

        return options;
    }
}