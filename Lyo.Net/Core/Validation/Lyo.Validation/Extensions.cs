using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Validation;

/// <summary>DI registration for schema-backed validation.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="InMemoryValidationSchemaStore" /> and <see cref="ValidationSchemaCompiler" />. Hosts that evaluate schemas must also register <see cref="IValidationClauseEvaluator" />.</summary>
        public IServiceCollection AddValidation()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IValidationSchemaStore, InMemoryValidationSchemaStore>();
            services.TryAddSingleton<IValidationSchemaCompiler, ValidationSchemaCompiler>();
            return services;
        }

#if NET
        /// <summary>Binds <see cref="IValidationClauseEvaluator" /> to <see cref="WhereClauseServiceEvaluator" />. Call <c>AddLyoQueryServices</c> first.</summary>
        public IServiceCollection AddQueryValidationEvaluator()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IValidationClauseEvaluator, WhereClauseServiceEvaluator>();
            return services;
        }
#endif
    }
}
