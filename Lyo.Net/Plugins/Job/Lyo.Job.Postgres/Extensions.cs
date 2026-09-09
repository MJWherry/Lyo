using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Configuration;
using Lyo.Encryption;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Security;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Events;
using Lyo.Job.Postgres.Mapping;
using Lyo.MessageQueue;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Job.Postgres;

/// <summary>Extension methods for registering the PostgreSQL job management database context.</summary>
public static class Extensions
{

    /// <param name="services">Service collection to register into</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers JobContext on the service collection.</summary>
        /// <param name="connectionString">PostgreSQL connection string</param>
        /// <returns>The service collection, for chaining</returns>
        public IServiceCollection AddJobDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddJobDbContextFactory(new PostgresJobOptions { ConnectionString = connectionString })
                .AddScoped<JobContext>(sp => sp.GetRequiredService<IDbContextFactory<JobContext>>().CreateDbContext());
        }

        /// <summary>Registers a PostgreSQL job management DbContextFactory on the service collection, with optional auto-migrations.</summary>
        /// <param name="configure">Action that configures the PostgreSQL job options</param>
        /// <returns>The service collection, for chaining</returns>
        public IServiceCollection AddJobDbContextFactory(Action<PostgresJobOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresJobOptions();
            configure(options);
            return services.AddJobDbContextFactory(options);
        }

        /// <summary>Registers a PostgreSQL job management DbContextFactory on the service collection via configuration binding.</summary>
        /// <param name="configuration">Configuration root (for example builder.Configuration)</param>
        /// <param name="configSectionName">Configuration section name (defaults to PostgresJobOptions.SectionName)</param>
        /// <returns>The service collection, for chaining</returns>
        public IServiceCollection AddJobDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresJobOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresJobOptions>(configuration, configSectionName);

            return services.AddJobDbContextFactory(options);
        }

        /// <summary>Registers a PostgreSQL job management DbContextFactory on the service collection, with optional auto-migrations.</summary>
        /// <param name="options">PostgreSQL job options</param>
        /// <returns>The service collection, for chaining</returns>
        public IServiceCollection AddJobDbContextFactory(PostgresJobOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<JobContext, PostgresJobOptions>(options);

            return services;
        }

        /// <summary>
        /// Registers job management on a PostgreSQL backend. Drop-and-play: registers DbContextFactory, auto-migrations (when enabled), CRUD services, and
        /// <see cref="JobLyoMapper" /> as <see cref="ILyoMapper" /> (hosts may replace with <see cref="Api.Mapping.CompositeLyoMapper" />). Requires AddLyoQueryServices plus
        /// AddFusionCache or AddLocalCache.
        /// </summary>
        public IServiceCollection AddPostgresJobManagement(Action<PostgresJobOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresJobOptions();
            configure(options);
            return services.AddPostgresJobManagement(options);
        }

        /// <summary>Registers job management on a PostgreSQL backend via configuration binding.</summary>
        public IServiceCollection AddPostgresJobManagementFromConfiguration(IConfiguration configuration, string configSectionName = PostgresJobOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = LyoOptions.Bind<PostgresJobOptions>(configuration, configSectionName);

            return services.AddPostgresJobManagement(options);
        }

        /// <summary>
        /// Registers job management on a PostgreSQL backend. Drop-and-play: registers DbContextFactory, auto-migrations (when enabled), CRUD services, and
        /// <see cref="JobLyoMapper" /> as <see cref="ILyoMapper" /> (hosts may replace with <see cref="Api.Mapping.CompositeLyoMapper" />). Requires AddLyoQueryServices plus
        /// AddFusionCache or AddLocalCache.
        /// </summary>
        public IServiceCollection AddPostgresJobManagement(PostgresJobOptions options)
        {
            services.AddJobDbContextFactory(options);
            services.AddLyoCrudServices<JobContext>();
            services.AddScoped<JobService>();
            services.TryAddSingleton<JobLyoMapper>();
            services.TryAddSingleton<ILyoMapper>(sp => sp.GetRequiredService<JobLyoMapper>());
            // Register a no-op publisher so JobService can be resolved without a message-queue transport.
            // Call AddMqJobEventPublisher() afterwards to swap this for a real implementation.
            services.TryAddSingleton<IJobEventPublisher, NullJobEventPublisher>();
            return services;
        }

        /// <summary>
        /// Registers <see cref="JobMaintenanceService" /> as a hosted background service. Automatically fails dead jobs (heartbeat timeout), resets circuit breakers, purges old
        /// run history per retention settings, and prunes stale worker instances. Requires <see cref="IDbContextFactory{JobContext}" /> to already be registered (call
        /// <see cref="AddJobDbContextFactory(IServiceCollection, PostgresJobOptions)" /> first).
        /// </summary>
        /// <param name="configure">Optional action that configures <see cref="JobMaintenanceOptions" />.</param>
        public IServiceCollection AddJobMaintenanceService(Action<JobMaintenanceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new JobMaintenanceOptions();
            configure?.Invoke(options);
            options.Validate();
            services.TryAddSingleton(options);
            return services.AddJobMaintenanceServiceCore();
        }

        /// <summary>
        /// Registers <see cref="JobMaintenanceService" /> like <see cref="AddJobMaintenanceService(IServiceCollection, Action{JobMaintenanceOptions}?)" />, binding
        /// <see cref="JobMaintenanceOptions" /> from configuration and validating them when the host starts.
        /// </summary>
        public IServiceCollection AddJobMaintenanceServiceFromConfiguration(IConfiguration configuration, string configSectionName = JobMaintenanceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddOptions<JobMaintenanceOptions>()
                .Bind(configuration.GetSection(configSectionName))
                .Validate(o => o.GetValidationErrors().Count == 0, $"Invalid {nameof(JobMaintenanceOptions)} — see {nameof(JobMaintenanceOptions.GetValidationErrors)}.")
                .ValidateOnStart();

            services.TryAddSingleton(p => p.GetRequiredService<IOptions<JobMaintenanceOptions>>().Value);
            return services.AddJobMaintenanceServiceCore();
        }

        private IServiceCollection AddJobMaintenanceServiceCore()
        {
            services.AddSingleton<JobMaintenanceService>();
            services.AddSingleton<IHealth>(p => p.GetRequiredService<JobMaintenanceService>());
            services.AddHostedService(p => p.GetRequiredService<JobMaintenanceService>());
            return services;
        }

        /// <summary>
        /// Registers <see cref="Events.MqJobEventPublisher" /> as the <see cref="IJobEventPublisher" /> for API hosts that have a job database. Scheduler and worker hosts must use
        /// <c>Lyo.Job.Client.AddMqJobEventPublisher*</c> instead. Requires <see cref="IMqService" />.
        /// </summary>
        public IServiceCollection AddMqJobEventPublisher()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddOptions<JobMqOptions>();
            return services.AddMqJobEventPublisherCore();
        }

        /// <summary>Registers the Postgres <see cref="Events.MqJobEventPublisher" /> with inline topology settings (API hosts only).</summary>
        /// <param name="configure">Configures queue topology.</param>
        public IServiceCollection AddMqJobEventPublisher(Action<JobMqOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new JobMqOptions();
            configure(options);
            return services.AddMqJobEventPublisher(options);
        }

        /// <summary>Registers the Postgres <see cref="Events.MqJobEventPublisher" /> with the given topology settings (API hosts only).</summary>
        /// <param name="options">Queue topology declared when the host starts.</param>
        public IServiceCollection AddMqJobEventPublisher(JobMqOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            return services.AddMqJobEventPublisherCore();
        }

        /// <summary>Registers the Postgres <see cref="Events.MqJobEventPublisher" /> and binds <see cref="JobMqOptions" /> from configuration. API hosts only.</summary>
        public IServiceCollection AddMqJobEventPublisherFromConfiguration(IConfiguration configuration, string configSectionName = JobMqOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddOptions<JobMqOptions>();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                services.Configure<JobMqOptions>(section);

            return services.AddMqJobEventPublisherCore();
        }

        private IServiceCollection AddMqJobEventPublisherCore()
        {
            services.TryAddSingleton(p => p.GetRequiredService<IOptions<JobMqOptions>>().Value);
            services.AddSingleton<IJobEventPublisher, MqJobEventPublisher>();
            services.AddHostedService<JobEventPublisherStartupService>();
            return services;
        }

        /// <summary>Registers <see cref="JobParameterEncryptionService" /> against an optional keyed <see cref="IEncryptionService" />.</summary>
        /// <param name="keyName">Keyed service name of the <see cref="IEncryptionService" />.</param>
        public IServiceCollection AddJobParameterEncryption(string keyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            services.TryAddSingleton<IJobParameterEncryptionService>(sp => {
                var encryption = sp.GetKeyedService<IEncryptionService>(keyName);
                return new JobParameterEncryptionService(encryption, keyName, sp.GetService<ILogger<JobParameterEncryptionService>>());
            });

            return services;
        }
    }
}