namespace Lyo.FeatureFlags;

/// <summary>Answers whether a named flag is on for a caller. A missing name is off. Names compare ordinal-ignore-case.</summary>
public interface IFeatureFlagService
{
    /// <summary>Returns true when <paramref name="name" /> is enabled for <paramref name="context" />. Whitespace names throw. A null context is an anonymous caller.</summary>
    bool IsEnabled(string name, FeatureFlagContext? context = null);
}
