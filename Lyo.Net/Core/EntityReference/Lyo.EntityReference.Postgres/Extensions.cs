using Lyo.EntityReference.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Lyo.Exceptions;

namespace Lyo.EntityReference.Postgres;

/// <summary>Host helpers that register global <see cref="EntityRefOptions" />.</summary>
public static class Extensions
{
    /// <summary>Default configuration section used by <see cref="AddEntityRefOptionsFromConfiguration" />.</summary>
    public const string DefaultSectionName = "EntityRef";

    /// <summary>Registers <see cref="EntityRefOptions" /> as <c>IOptions&lt;EntityRefOptions&gt;</c> and as <see cref="EntityRefOptions" />.</summary>
    /// <param name="services">Collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddEntityRefOptions(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddOptions<EntityRefOptions>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<EntityRefOptions>>().Value);
        return services;
    }

    /// <summary>Binds <see cref="EntityRefOptions" /> from configuration and checks the default tenant id is non-empty (validated on start).</summary>
    /// <param name="services">Collection to register into.</param>
    /// <param name="configuration">Configuration root.</param>
    /// <param name="section">Section name; defaults to <see cref="DefaultSectionName" />.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddEntityRefOptionsFromConfiguration(this IServiceCollection services, IConfiguration configuration, string section = DefaultSectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        services.AddOptions<EntityRefOptions>()
            .Bind(configuration.GetSection(section))
            .Validate(o => o.DefaultTenantId != Guid.Empty, $"{section}:{nameof(EntityRefOptions.DefaultTenantId)} cannot be empty.")
            .ValidateOnStart();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<EntityRefOptions>>().Value);

        return services;
    }
}