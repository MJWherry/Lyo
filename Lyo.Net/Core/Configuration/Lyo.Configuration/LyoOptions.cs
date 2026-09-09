using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Lyo.Configuration;

/// <summary>Maps a configuration section onto a new options instance.</summary>
/// <remarks>
/// Every Lyo package ships an <c>Add{Feature}FromConfiguration</c> registration, and they all need the same behavior: start from the options type's defaults, overlay
/// whatever the configuration section provides, and treat a missing section as "defaults are fine" rather than an error. That last part is why this helper exists instead
/// of a bare <c>section.Bind(options)</c> call — binding a missing section already works silently, but checking <c>Exists()</c> first makes the intent explicit and keeps
/// the behavior the same across packages.
/// </remarks>
public static class LyoOptions
{
    /// <summary>Creates <typeparamref name="TOptions" />, overlays <paramref name="sectionName" /> when it exists, then runs <paramref name="configure" />.</summary>
    /// <remarks>A missing or empty section is not an error: the returned instance keeps the type's declared defaults.</remarks>
    /// <param name="configuration">Configuration to read.</param>
    /// <param name="sectionName">Section to bind, typically <c>TOptions.SectionName</c>.</param>
    /// <param name="configure">Runs after binding, so code-supplied values beat configuration.</param>
    /// <returns>A filled options instance.</returns>
    public static TOptions Bind<TOptions>(IConfiguration configuration, string sectionName, Action<TOptions>? configure = null)
        where TOptions : new()
    {
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);

        var options = new TOptions();
        var section = configuration.GetSection(sectionName);
        if (section.Exists())
            section.Bind(options);

        configure?.Invoke(options);
        return options;
    }
}
