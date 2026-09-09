using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Common.Core.Enums;
using Lyo.Configuration;
using Lyo.Diff;
using Lyo.Drift.Postgres.Database;
using Lyo.Drift.Postgres.Mapping;
using Lyo.Exceptions;
using Lyo.Hashing;
using Lyo.Hashing.Registration;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Drift.Postgres;

/// <summary>DI registration for the Drift collector store.</summary>
public static class Extensions
{

    extension(IServiceCollection services)
    {
        /// <summary>Registers Drift DbContext factory, CRUD services, mapper, diff, and <see cref="DriftService" />.</summary>
        public IServiceCollection AddPostgresDriftManagement(Action<PostgresDriftOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresDriftOptions();
            configure(options);
            return services.AddPostgresDriftManagement(options);
        }

        /// <summary>Registers Drift from configuration section <see cref="PostgresDriftOptions.SectionName" />.</summary>
        public IServiceCollection AddPostgresDriftManagementFromConfiguration(IConfiguration configuration, string configSectionName = PostgresDriftOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = LyoOptions.Bind<PostgresDriftOptions>(configuration, configSectionName);
            return services.AddPostgresDriftManagement(options);
        }

        /// <summary>Registers Drift DbContext factory, CRUD services, mapper, diff, and <see cref="DriftService" />.</summary>
        public IServiceCollection AddPostgresDriftManagement(PostgresDriftOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<DriftDbContext, PostgresDriftOptions>(options);
            services.AddLyoHashing(o => o.DefaultHexLetterCase = TextLetterCase.Lower);
            services.AddLyoCrudServices<DriftDbContext>();
            services.AddLyoDiff();
            services.TryAddSingleton<DriftLyoMapper>();
            services.TryAddSingleton<ILyoMapper>(sp => sp.GetRequiredService<DriftLyoMapper>());
            services.TryAddScoped<DriftService>();
            return services;
        }

        /// <summary>Registers <see cref="DriftRetentionService" /> as a hosted background prune.</summary>
        public IServiceCollection AddDriftRetentionService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<DriftRetentionService>();
            services.AddHostedService(sp => sp.GetRequiredService<DriftRetentionService>());
            return services;
        }
    }
}
