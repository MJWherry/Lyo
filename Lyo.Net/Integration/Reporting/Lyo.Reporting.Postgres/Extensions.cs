using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Csv;
using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Reporting.Models.Profiles;
using Lyo.Reporting.Models.Providers;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Postgres.Database;
using Lyo.Reporting.Postgres.Mapping;
using Lyo.Reporting.Postgres.Rendering;
using Lyo.Xlsx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Reporting.Postgres;

/// <summary>DI registration for PostgreSQL reporting (service layer only — endpoints live in Lyo.Api.Reporting).</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddReportingDbContextFactory(Action<PostgresReportingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresReportingOptions();
            configure(options);
            return services.AddReportingDbContextFactory(options);
        }

        public IServiceCollection AddReportingDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresReportingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresReportingOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddReportingDbContextFactory(options);
        }

        public IServiceCollection AddReportingDbContextFactory(PostgresReportingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<ReportingContext, PostgresReportingOptions>();
            services.AddDbContextFactory<ReportingContext>(
                dbOptions => dbOptions.UseNpgsql(
                    options.ConnectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresReportingOptions.Schema)));
            return services;
        }

        public IServiceCollection AddPostgresReportingManagement(Action<PostgresReportingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresReportingOptions();
            configure(options);
            return services.AddPostgresReportingManagement(options);
        }

        public IServiceCollection AddPostgresReportingManagementFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresReportingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresReportingOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresReportingManagement(options);
        }

        /// <summary>
        /// Drop-and-play reporting: DbContextFactory, migrations, CRUD services, mapper, CSV/XLSX/JSON <see cref="IReportRenderer"/>s,
        /// <see cref="ReportService"/>, <see cref="ReportGenerationThrottle"/>, and <see cref="ReportRetentionService"/>.
        /// Requires AddLyoQueryServices, cache, and IIOTempService for generate staging.
        /// HTML/PDF: call <c>AddReportingWebRenderer()</c> from Lyo.Reporting.Web on the host.
        /// Persist outputs: <see cref="AddReportingGenerationHooks"/>.
        /// Map HTTP endpoints via <c>Lyo.Api.Reporting</c> <c>BuildReportingGroup</c>.
        /// </summary>
        public IServiceCollection AddPostgresReportingManagement(PostgresReportingOptions options)
        {
            services.AddReportingDbContextFactory(options);
            services.AddLyoCrudServices<ReportingContext>();
            services.AddCsvService();
            services.AddXlsxService();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportRenderer, CsvReportRenderer>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportRenderer, XlsxReportRenderer>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportRenderer, JsonReportRenderer>());
            services.AddScoped<ReportService>();
            services.AddScoped<ReportRetentionService>();
            services.TryAddSingleton<ReportGenerationThrottle>();
            services.TryAddSingleton<ReportingLyoMapper>();
            services.TryAddSingleton<ILyoMapper>(sp => sp.GetRequiredService<ReportingLyoMapper>());
            return services;
        }

        /// <summary>
        /// Opt-in hosted worker that runs <see cref="ReportRetentionService"/> maintenance (retention cleanup and
        /// stuck-generation recovery) every <see cref="PostgresReportingOptions.MaintenanceInterval"/>.
        /// Call after <see cref="AddPostgresReportingManagement(PostgresReportingOptions)"/>. Not needed on hosts
        /// that already schedule <see cref="ReportRetentionService.CleanupAsync(CancellationToken)"/> themselves.
        /// </summary>
        public IServiceCollection AddReportingMaintenanceWorker()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddHostedService<ReportingMaintenanceService>();
            return services;
        }

        /// <summary>
        /// Registers host <see cref="ReportGenerationHooks"/> used by <see cref="ReportService"/>
        /// (e.g. save staged output via FileStorage). Replaces any previously registered hooks instance.
        /// </summary>
        public IServiceCollection AddReportingGenerationHooks(ReportGenerationHooks hooks)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(hooks);
            services.RemoveAll<ReportGenerationHooks>();
            services.AddSingleton(hooks);
            return services;
        }

        /// <summary>Registers a generation profile (defaults for format/filename/path) keyed by <see cref="ReportingGenerationProfile.Key"/>.</summary>
        /// <remarks>
        /// Uses <see cref="ServiceCollectionServiceExtensions.AddSingleton{TService}(IServiceCollection, TService)"/> rather than
        /// <c>TryAddEnumerable</c>: profiles are the same concrete type, which DI rejects as indistinguishable for enumerable registration.
        /// All registered profiles are still available via <c>IEnumerable&lt;ReportingGenerationProfile&gt;</c>.
        /// </remarks>
        public IServiceCollection AddReportingGenerationProfile(ReportingGenerationProfile profile)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(profile);
            if (string.IsNullOrWhiteSpace(profile.Key))
                throw new ArgumentException("Profile Key is required.", nameof(profile));
            services.AddSingleton(profile);
            return services;
        }

        /// <summary>Registers a generation profile by key.</summary>
        public IServiceCollection AddReportingGenerationProfile(string key, Action<ReportingGenerationProfileBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(configure);
            var builder = new ReportingGenerationProfileBuilder(key);
            configure(builder);
            return services.AddReportingGenerationProfile(builder.Build());
        }

        /// <summary>Registers an <see cref="IReportDataProvider"/> (typically on the API host).</summary>
        public IServiceCollection AddReportDataProvider<TProvider>()
            where TProvider : class, IReportDataProvider
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportDataProvider, TProvider>());
            return services;
        }

        /// <summary>Registers an <see cref="IReportDataProvider"/> instance.</summary>
        public IServiceCollection AddReportDataProvider(IReportDataProvider provider)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(provider);
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IReportDataProvider), provider));
            return services;
        }
    }
}

/// <summary>Fluent builder for <see cref="ReportingGenerationProfile"/>.</summary>
public sealed class ReportingGenerationProfileBuilder(string key)
{
    private Models.Enums.ReportFormat? _defaultFormat;
    private string? _defaultFileName;
    private string? _defaultPathPrefix;

    public ReportingGenerationProfileBuilder DefaultFormat(Models.Enums.ReportFormat format)
    {
        _defaultFormat = format;
        return this;
    }

    public ReportingGenerationProfileBuilder DefaultFileName(string fileName)
    {
        _defaultFileName = fileName;
        return this;
    }

    public ReportingGenerationProfileBuilder DefaultPathPrefix(string pathPrefix)
    {
        _defaultPathPrefix = pathPrefix;
        return this;
    }

    public ReportingGenerationProfile Build()
        => new() {
            Key = key,
            DefaultFormat = _defaultFormat,
            DefaultFileName = _defaultFileName,
            DefaultPathPrefix = _defaultPathPrefix
        };
}
