using Lyo.Query.Services.PropertyComparison;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Query;

/// <summary>Extension methods for registering Lyo.Query services. Requires CacheService and CacheOptions to be registered.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers IValueConversionService, IPropertyComparisonService, and IWhereClauseService with their default implementations. Requires CacheService and CacheOptions to be
    /// registered (e.g. via AddFusionCache or AddLocalCache).
    /// </summary>
    /// <param name="registerValueConversion">When true (default), registers ValueConversionService. Set to false when using Lyo.Api's TypeConversionService as IValueConversionService.</param>
    public static IServiceCollection AddLyoQueryServices(this IServiceCollection services, bool registerValueConversion = true)
    {
        if (registerValueConversion)
            services.AddSingleton<IValueConversionService, ValueConversionService>();

        services.AddSingleton<IPropertyComparisonService, PropertyComparisonService>().AddSingleton<IWhereClauseService, BaseWhereClauseService>();
        return services;
    }
}