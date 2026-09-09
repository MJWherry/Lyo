namespace Lyo.Config.Web.Components;

/// <summary>Source of a resolved config value came from.</summary>
public enum ConfigValueSource
{
    /// <summary>Value comes from an entity-specific binding.</summary>
    Binding,

    /// <summary>No binding; the definition default applies.</summary>
    Default,

    /// <summary>Neither a binding nor a default is present.</summary>
    Missing
}

/// <summary>MudBlazor palette for config required/source chips. Rendering goes through <see cref="LyoChip" />.</summary>
public static class ConfigColorHelper
{
    /// <summary>Chip color for required vs optional.</summary>
    public static Color ForRequired(bool isRequired) => isRequired ? Color.Warning : Color.Default;

    /// <summary>Chip color for the value source.</summary>
    public static Color ForSource(ConfigValueSource source)
        => source switch {
            ConfigValueSource.Binding => Color.Success,
            ConfigValueSource.Default => Color.Info,
            ConfigValueSource.Missing => Color.Error,
            var _ => Color.Default
        };

    /// <summary>A resolved-value source chip short label.</summary>
    public static string SourceLabel(ConfigValueSource source)
        => source switch {
            ConfigValueSource.Binding => "Overridden",
            ConfigValueSource.Default => "Using default",
            ConfigValueSource.Missing => "Missing",
            var _ => "—"
        };

    /// <summary>Looks up source from a merged config item.</summary>
    public static ConfigValueSource ResolveSource(ResolvedConfigItemRecord item)
    {
        if (item.Binding != null)
            return ConfigValueSource.Binding;

        if (item.Definition.DefaultValue != null)
            return ConfigValueSource.Default;

        return ConfigValueSource.Missing;
    }
}
