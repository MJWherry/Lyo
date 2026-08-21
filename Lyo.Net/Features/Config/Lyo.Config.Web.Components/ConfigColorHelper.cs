namespace Lyo.Config.Web.Components;

/// <summary>Where a resolved config value came from.</summary>
public enum ConfigValueSource
{
    /// <summary>An entity-specific binding supplies the value.</summary>
    Binding,

    /// <summary>No binding; the definition default is in effect.</summary>
    Default,

    /// <summary>No binding and no default.</summary>
    Missing
}

/// <summary>MudBlazor colors for config required/source chips. Rendering goes through <see cref="LyoChip" />.</summary>
public static class ConfigColorHelper
{
    /// <summary>Color for the required/optional chip.</summary>
    public static Color ForRequired(bool isRequired) => isRequired ? Color.Warning : Color.Default;

    /// <summary>Color for a resolved-value source chip.</summary>
    public static Color ForSource(ConfigValueSource source)
        => source switch {
            ConfigValueSource.Binding => Color.Success,
            ConfigValueSource.Default => Color.Info,
            ConfigValueSource.Missing => Color.Error,
            var _ => Color.Default
        };

    /// <summary>Short label for a resolved-value source chip.</summary>
    public static string SourceLabel(ConfigValueSource source)
        => source switch {
            ConfigValueSource.Binding => "Binding",
            ConfigValueSource.Default => "Default",
            ConfigValueSource.Missing => "Missing",
            var _ => "—"
        };

    /// <summary>Resolves source from a merged config item.</summary>
    public static ConfigValueSource ResolveSource(ResolvedConfigItemRecord item)
    {
        if (item.Binding != null)
            return ConfigValueSource.Binding;

        if (item.Definition.DefaultValue != null)
            return ConfigValueSource.Default;

        return ConfigValueSource.Missing;
    }
}
