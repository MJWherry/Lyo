using System.Text.Json;
using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Postgres;
using Lyo.Schedule.Models;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UUIDNext;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Postgres;

/// <summary>Extension methods for PostgreSQL job management database context registration.</summary>
public static class Extensions
{
    /// <summary>Adds JobContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJobDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddJobDbContextFactory(new PostgresJobOptions { ConnectionString = connectionString })
            .AddScoped<JobContext>(sp => sp.GetRequiredService<IDbContextFactory<JobContext>>().CreateDbContext());
    }

    /// <summary>Adds JobContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJobDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<JobContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL job management DbContextFactory to the service collection with optional auto-migrations.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL job options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJobDbContextFactory(this IServiceCollection services, Action<PostgresJobOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresJobOptions();
        configure(options);
        return services.AddJobDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL job management DbContextFactory to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresJobOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJobDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresJobOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresJobOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddJobDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL job management DbContextFactory to the service collection with optional auto-migrations.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL job options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJobDbContextFactory(this IServiceCollection services, PostgresJobOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresJobOptions>>(Options.Create(options));
        services.AddPostgresMigrations<JobContext, PostgresJobOptions>();
        services.AddDbContextFactory<JobContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresJobOptions.Schema)));

        return services;
    }

    /// <summary>
    /// Adds job management with PostgreSQL backend. Drop-and-play: registers DbContextFactory, auto-migrations (if enabled), and CRUD services. Requires: AddLyoQueryServices,
    /// AddFusionCache or AddLocalCache, MapsterMapper.IMapper (add mapping yourself).
    /// </summary>
    public static IServiceCollection AddPostgresJobManagement(this IServiceCollection services, Action<PostgresJobOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresJobOptions();
        configure(options);
        return services.AddPostgresJobManagement(options);
    }

    /// <summary>Adds job management with PostgreSQL backend using configuration binding.</summary>
    public static IServiceCollection AddPostgresJobManagementFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresJobOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        var options = new PostgresJobOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresJobManagement(options);
    }

    /// <summary>
    /// Adds job management with PostgreSQL backend. Drop-and-play: registers DbContextFactory, auto-migrations (if enabled), and CRUD services. Requires: AddLyoQueryServices,
    /// AddFusionCache or AddLocalCache, MapsterMapper.IMapper (add mapping yourself).
    /// </summary>
    public static IServiceCollection AddPostgresJobManagement(this IServiceCollection services, PostgresJobOptions options)
    {
        services.AddJobDbContextFactory(options);
        services.AddLyoCrudServices<JobContext>();
        return services;
    }

    /// <summary>Maps job API endpoints. Call after AddPostgresJobManagement.</summary>
    public static WebApplication BuildJobGroup(this WebApplication app)
    {
        app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>(Constants.Rest.Job.Definitions, "Job")
            .WithCrud(
                ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql),
                    AfterUpdate = ctx => {
                        var mq = app.Services.GetRequiredService<IRabbitMqService>();
                        mq.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobDefinitionChangeKey, JsonSerializer.SerializeToUtf8Bytes(ctx.Entity.Id))
                            .GetAwaiter()
                            .GetResult();
                    },
                    BeforeDelete = ctx => {
                        var i = ctx.Entity;
                        var db = ctx.DbContext;
                        foreach (var jobRun in i.JobRuns) {
                            foreach (var x in jobRun.InverseReRanFromJobRun)
                                x.ReRanFromJobRunId = null;

                            db.JobRunLogs.RemoveRange(jobRun.JobRunLogs);
                            db.JobRunParameters.RemoveRange(jobRun.JobRunParameters);
                            db.JobRunResults.RemoveRange(jobRun.JobRunResults);
                        }

                        db.JobRuns.RemoveRange(i.JobRuns);
                        foreach (var schedule in i.JobSchedules)
                            db.JobScheduleParameters.RemoveRange(schedule.JobScheduleParameters);

                        db.JobSchedules.RemoveRange(i.JobSchedules);
                        db.JobParameters.RemoveRange(i.JobParameters);
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobParameter, JobParameterReq, JobParameterRes, Guid>($"{Constants.Rest.Job.DefinitionParameters}", "Job")
            .WithCrud(
                ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate,
                new() {
                    BeforeCreate = ctx => ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql),
                    AfterUpdate = ctx => app.Services.GetRequiredService<IRabbitMqService>()
                        .SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobDefinitionChangeKey, JsonSerializer.SerializeToUtf8Bytes(ctx.Entity.JobDefinitionId))
                        .GetAwaiter()
                        .GetResult()
                })
            .Build();

        app.CreateBuilder<JobContext, JobSchedule, JobScheduleReq, JobScheduleRes, Guid>($"{Constants.Rest.Job.Schedules}", "Job")
            .WithCrud(
                ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql),
                    AfterUpdate = ctx => {
                        var mq = app.Services.GetRequiredService<IRabbitMqService>();
                        mq.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobDefinitionChangeKey, JsonSerializer.SerializeToUtf8Bytes(ctx.Entity.JobDefinitionId))
                            .GetAwaiter()
                            .GetResult();
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobTrigger, JobTriggerReq, JobTriggerRes, Guid>($"{Constants.Rest.Job.Triggers}", "Job")
            .WithCrud(
                ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql),
                    AfterUpdate = ctx => {
                        var mq = app.Services.GetRequiredService<IRabbitMqService>();
                        mq.SendToExchange(
                                Constants.Mq.JobEventExchange, Constants.Mq.JobDefinitionChangeKey, JsonSerializer.SerializeToUtf8Bytes(ctx.Entity.TriggersJobDefinitionId))
                            .GetAwaiter()
                            .GetResult();

                        mq.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobDefinitionChangeKey, JsonSerializer.SerializeToUtf8Bytes(ctx.Entity.JobDefinitionId))
                            .GetAwaiter()
                            .GetResult();
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobRun, JobRunReq, JobRunRes, Guid>(Constants.Rest.Job.Runs, "Job")
            .WithQuery()
            .WithGet()
            .WithDelete(
                ctx => {
                    var jobRun = ctx.Entity;
                    var db = ctx.DbContext;
                    foreach (var i in jobRun.InverseReRanFromJobRun)
                        i.ReRanFromJobRunId = null;

                    db.JobRunLogs.RemoveRange(jobRun.JobRunLogs);
                    db.JobRunParameters.RemoveRange(jobRun.JobRunParameters);
                    db.JobRunResults.RemoveRange(jobRun.JobRunResults);
                }, null, ["JobRunLogs", "JobRunParameters", "JobRunResults", "InverseReRanFromJobRun"])
            .WithDeleteBulk(
                ctx => {
                    var jobRun = ctx.Entity;
                    var db = ctx.DbContext;
                    db.JobRunLogs.RemoveRange(jobRun.JobRunLogs);
                    db.JobRunParameters.RemoveRange(jobRun.JobRunParameters);
                    db.JobRunResults.RemoveRange(jobRun.JobRunResults);
                }, null, ["JobRunLogs", "JobRunParameters", "JobRunResults", "InverseReRanFromJobRun"])
            .Build();

        app.CreateBuilder<JobContext, JobRunParameter, JobRunParameterReq, JobRunParameterRes, Guid>(Constants.Rest.Job.RunParameters, "Job")
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql))
            .Build();

        app.CreateBuilder<JobContext, JobRunResult, JobRunResultRes, JobRunResultRes, Guid>(Constants.Rest.Job.RunResults, "Job")
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql))
            .Build();

        app.CreateBuilder<JobContext, JobRunLog, JobRunLogReq, JobRunLogRes, Guid>(Constants.Rest.Job.RunLogs, "Job")
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql))
            .Build();

        return app;
    }

    /// <summary>Configures Mapster job entity mappings. Call when configuring Mapster (e.g. config.Apply(ConfigureJobMappings)).</summary>
    public static TypeAdapterConfig ConfigureJobMappings(this TypeAdapterConfig config)
    {
        config.NewConfig<JobDefinitionReq, JobDefinition>()
            .Map(to => to.JobParameters, from => from.CreateParameters)
            .Map(to => to.JobSchedules, from => from.CreateSchedules)
            .Map(to => to.JobTriggerJobDefinitions, from => from.CreateTriggers);

        config.NewConfig<JobDefinition, JobDefinitionRes>().Map(to => to.JobTriggers, from => from.JobTriggerJobDefinitions);
        config.NewConfig<JobSchedule, JobScheduleRes>()
            .Map(dest => dest.Times, src => (src.Times ?? new List<string>()).Select(TimeOnly.Parse).ToArray())
            .Map(dest => dest.Type, src => Enum.Parse<ScheduleType>(src.Type))
            .Map(dest => dest.DayFlags, src => Enum.Parse<DayFlags>(src.DayFlags))
            .Map(dest => dest.MonthFlags, src => Enum.Parse<MonthFlags>(src.MonthFlags));

        config.NewConfig<JobScheduleReq, JobSchedule>()
            .Map(dest => dest.Times, src => (src.Times ?? Enumerable.Empty<TimeOnly>()).Select(i => i.ToString()).ToList())
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.DayFlags, src => src.DayFlags.ToString())
            .Map(dest => dest.MonthFlags, src => src.MonthFlags.ToString());

        config.NewConfig<JobTriggerReq, JobTrigger>()
            .Map(dest => dest.TriggerComparator, from => from.Comparison)
            .Map(dest => dest.TriggerJobResultKey, from => from.JobResultKey)
            .Map(dest => dest.TriggerJobResultValue, from => from.JobResultValue)
            .Map(dest => dest.JobTriggerParameters, from => from.CreateTriggerParameters);

        config.NewConfig<JobTrigger, JobTriggerRes>()
            .MaxDepth(4)
            .Map(dest => dest.Comparison, from => from.TriggerComparator)
            .Map(dest => dest.JobResultKey, from => from.TriggerJobResultKey)
            .Map(dest => dest.JobResultValue, from => from.TriggerJobResultValue)
            .Map(dest => dest.TriggerParameters, from => from.JobTriggerParameters);

        return config;
    }
}