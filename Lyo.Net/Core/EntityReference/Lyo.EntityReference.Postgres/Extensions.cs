using Lyo.EntityReference.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.EntityReference.Postgres;

/// <summary>Host extension methods for global <see cref="EntityRefOptions" /> registration.</summary>
public static class Extensions
{
    /// <summary>Default configuration section bound by <see cref="AddEntityRefOptionsFromConfiguration" />.</summary>
    public const string DefaultSectionName = "EntityRef";

    /// <summary>Binds <see cref="EntityRefOptions" /> from configuration and validates the default tenant id is non-empty (validated on start).</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration root.</param>
    /// <param name="section">Configuration section name (defaults to <see cref="DefaultSectionName" />).</param>
    /// <returns><paramref name="services" /> for chaining.</returns>
    public static IServiceCollection AddEntityRefOptionsFromConfiguration(this IServiceCollection services, IConfiguration configuration, string section = DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        services.AddOptions<EntityRefOptions>()
            .Bind(configuration.GetSection(section))
            .Validate(o => o.DefaultTenantId != Guid.Empty, $"{section}:{nameof(EntityRefOptions.DefaultTenantId)} cannot be empty.")
            .ValidateOnStart();

        return services;
    }
}