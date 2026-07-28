using Lyo.Api.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api;

public static partial class ServiceCollectionExtensions
{
    private static void ApplyLyoNavigations<TContext>(IServiceProvider sp, DbContextOptionsBuilder builder)
        where TContext : DbContext
    {
        builder.ReplaceService<IModelCustomizer, LyoComposingModelCustomizer>();
        var navOptions = sp.GetRequiredService<CrossSchemaNavigationOptions<TContext>>();
        if (navOptions.Registrations.Count == 0)
            return;

        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new CrossSchemaNavigationOptionsExtension(navOptions.Registrations.ToArray()));
        builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers same-context and cross-schema navigations for <typeparamref name="TContext" />. Pair with <see cref="AddDbContextFactoryWithLyoNavigations{TContext}" /> so the
        /// model customizer applies them. Related tables must live in the same database; cross-schema mappings use <c>ExcludeFromMigrations</c>.
        /// </summary>
        public IServiceCollection AddCrossSchemaNavigations<TContext>(Action<CrossSchemaNavigationBuilder<TContext>> configure)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(configure);
            var options = new CrossSchemaNavigationOptions<TContext>();
            configure(new(options));
            services.RemoveAll<CrossSchemaNavigationOptions<TContext>>();
            services.AddSingleton(options);
            return services;
        }

        /// <summary>
        /// Registers <see cref="IDbContextFactory{TContext}" /> with <see cref="LyoComposingModelCustomizer" /> so <see cref="AddCrossSchemaNavigations{TContext}" /> registrations
        /// are applied after <c>OnModelCreating</c>. Call <see cref="AddCrossSchemaNavigations{TContext}" /> before this method. When registrations exist, ignores
        /// <see cref="RelationalEventId.PendingModelChangesWarning" /> so soft/cross-schema navigations do not block <c>MigrateAsync</c> against an unchanged migration snapshot.
        /// </summary>
        public IServiceCollection AddDbContextFactoryWithLyoNavigations<TContext>(Action<DbContextOptionsBuilder> optionsAction)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(optionsAction);
            services.TryAddSingleton(_ => new CrossSchemaNavigationOptions<TContext>());
            services.AddDbContextFactory<TContext>((sp, builder) => {
                optionsAction(builder);
                ApplyLyoNavigations<TContext>(sp, builder);
            });

            return services;
        }

        /// <summary>Registers <see cref="IDbContextFactory{TContext}" /> with typed options and <see cref="LyoComposingModelCustomizer" />.</summary>
        public IServiceCollection AddDbContextFactoryWithLyoNavigations<TContext>(Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(optionsAction);
            services.TryAddSingleton(_ => new CrossSchemaNavigationOptions<TContext>());
            services.AddDbContextFactory<TContext>((sp, builder) => {
                optionsAction(sp, builder);
                ApplyLyoNavigations<TContext>(sp, builder);
            });

            return services;
        }
    }
}