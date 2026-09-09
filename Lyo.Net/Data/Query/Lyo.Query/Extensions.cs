using Lyo.Exceptions;
using Lyo.Query.Services.PropertyComparison;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Query;

/// <summary>DI helpers that register Lyo.Query services. CacheService and CacheOptions must already be registered.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers IValueConversionService, IPropertyComparisonService, and IWhereClauseService with their stock implementations. CacheService and CacheOptions must already
    /// be registered, for example through AddFusionCache or AddLocalCache.
    /// </summary>
    /// <param name="services">Collection to add the query services to.</param>
    /// <param name="registerValueConversion">
    /// If true (the default), registers ValueConversionService. Pass false when Lyo.Api's TypeConversionService is already bound as IValueConversionService.
    /// </param>
    public static IServiceCollection AddLyoQueryServices(this IServiceCollection services, bool registerValueConversion = true)
    {
        ArgumentHelpers.ThrowIfNull(services);
        if (registerValueConversion)
            services.AddSingleton<IValueConversionService, ValueConversionService>();

        services.AddSingleton<IPropertyComparisonService, PropertyComparisonService>().AddSingleton<IWhereClauseService, BaseWhereClauseService>();
        return services;
    }
}