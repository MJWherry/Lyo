using Lyo.Exceptions;
using Lyo.Query.Services.WhereClause;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Query.Postgres;

/// <summary>DI helpers that swap the default where-clause service for the Postgres Fuzzy translator.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Replaces <see cref="IWhereClauseService" /> with <see cref="PostgresWhereClauseService" /> so Fuzzy / NotFuzzy conditions translate to <c>similarity()</c>. Call after
        /// <c>AddLyoQueryServices()</c>. Requires <c>CREATE EXTENSION pg_trgm</c> on the database. GIN/GiST <c>gin_trgm_ops</c> indexes are operator-owned and are not created here.
        /// </summary>
        /// <returns>The same collection so calls can be chained.</returns>
        public IServiceCollection AddLyoQueryPostgres()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.Replace(ServiceDescriptor.Singleton<IWhereClauseService, PostgresWhereClauseService>());
            return services;
        }
    }
}