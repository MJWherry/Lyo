using Lyo.Exceptions;
using Lyo.Validation.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Validation;

/// <summary>Helpers that register schema-backed validation.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="InMemoryValidationSchemaStore" /> and <see cref="ValidationSchemaCompiler" />. Hosts that evaluate schemas must also register
        /// <see cref="IValidationClauseEvaluator" />.
        /// </summary>
        public IServiceCollection AddValidation()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IValidationSchemaStore, InMemoryValidationSchemaStore>();
            services.TryAddSingleton<IValidationSchemaCompiler, ValidationSchemaCompiler>();
            return services;
        }

        /// <summary>
        /// Binds <see cref="IValidationClauseEvaluator" /> to <see cref="WhereClauseServiceEvaluator" />. Register an <c>IWhereClauseService</c> first —
        /// <c>AddLyoQueryServices</c> does, or bind <see cref="Query.Services.WhereClause.WhereClauseEvaluator" /> directly for a cache-free evaluator.
        /// </summary>
        public IServiceCollection AddQueryValidationEvaluator()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IValidationClauseEvaluator, WhereClauseServiceEvaluator>();
            return services;
        }
    }
}
