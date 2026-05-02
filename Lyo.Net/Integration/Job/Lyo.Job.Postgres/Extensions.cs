using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Common.Enums;
using Lyo.Common.Identifiers;
using Lyo.Exceptions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Events;
using Lyo.MessageQueue;
using Lyo.Postgres;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Constants = Lyo.Job.Models.Constants;
using JobRunResult = Lyo.Job.Postgres.Database.JobRunResult;

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
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        return services.AddJobDbContextFactory(new PostgresJobOptions { ConnectionString = connectionString })
            .AddScoped<JobContext>(sp => sp.GetRequiredService<IDbContextFactory<JobContext>>().CreateDbContext());
    }

    /// <summary>Adds JobContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJobDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        services.AddDbContext<JobContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL job management DbContextFactory to the service collection with optional auto-migrations.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL job options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJobDbContextFactory(this IServiceCollection services, Action<PostgresJobOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
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
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
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
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
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
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
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
        ArgumentHelpers.ThrowIfNull(configuration);
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
        services.AddScoped<JobService>();
        // Register a no-op publisher so JobService can be resolved without a message-queue transport.
        // Call AddMqJobEventPublisher() afterwards to replace this with a real implementation.
        services.TryAddSingleton<IJobEventPublisher, NullJobEventPublisher>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="JobMaintenanceService" /> as a hosted background service. Automatically fails dead jobs (heartbeat timeout) and resets circuit breakers. Requires
    /// <see cref="IDbContextFactory{JobContext}" /> to be registered (call <see cref="AddJobDbContextFactory(IServiceCollection, PostgresJobOptions)" /> first).
    /// </summary>
    public static IServiceCollection AddJobMaintenanceService(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddHostedService<JobMaintenanceService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="MqJobEventPublisher" /> as the <see cref="IJobEventPublisher" /> implementation. Requires <see cref="IMqService" /> to already be registered (e.g.
    /// via <c>AddRabbitMq</c>).
    /// </summary>
    public static IServiceCollection AddMqJobEventPublisher(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddSingleton<IJobEventPublisher, MqJobEventPublisher>();
        return services;
    }

    /// <summary>Maps job API endpoints. Call after AddPostgresJobManagement.</summary>
    public static WebApplication BuildJobGroup(this WebApplication app)
    {
        app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>(Constants.Rest.Job.Definitions, "Job")
            .WithCrud(
                ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    AfterUpdate = ctx => {
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.Id).GetAwaiter().GetResult();
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
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    AfterUpdate = ctx => app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult()
                })
            .Build();

        app.CreateBuilder<JobContext, JobSchedule, JobScheduleReq, JobScheduleRes, Guid>($"{Constants.Rest.Job.Schedules}", "Job")
            .WithCrud(
                ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    AfterUpdate = ctx => {
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobTrigger, JobTriggerReq, JobTriggerRes, Guid>($"{Constants.Rest.Job.Triggers}", "Job")
            .WithCrud(
                ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    AfterUpdate = ctx => {
                        var publisher = app.Services.GetRequiredService<IJobEventPublisher>();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.TriggersJobDefinitionId).GetAwaiter().GetResult();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
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
            .WithCreate(ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres())
            .Build();

        app.CreateBuilder<JobContext, JobRunResult, JobRunResultRes, JobRunResultRes, Guid>(Constants.Rest.Job.RunResults, "Job")
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres())
            .Build();

        app.CreateBuilder<JobContext, JobRunLog, JobRunLogReq, JobRunLogRes, Guid>(Constants.Rest.Job.RunLogs, "Job")
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres())
            .Build();

        MapStatsEndpoint(app);
        return app;
    }

    /// <summary>Maps the <c>GET /Job/Definition/{id}/Stats</c> endpoint. Called by <see cref="BuildJobGroup" />.</summary>
    private static void MapStatsEndpoint(WebApplication app)
        => app.MapGet(
                $"/{Constants.Rest.Job.Definitions}/{{id:guid}}/Stats", async (Guid id, int days, JobService jobService, CancellationToken ct) => {
                    days = days > 0 ? days : 30;
                    var stats = await jobService.GetDefinitionStats(id, days, ct).ConfigureAwait(false);
                    return stats is null ? Results.NotFound() : Results.Ok(stats);
                })
            .WithTags("Job")
            .WithName("GetJobDefinitionStats");

    /// <summary>Configures Mapster job entity mappings. Call when configuring Mapster (e.g. config.Apply(ConfigureJobMappings)).</summary>
    /// <remarks>
    /// All entity→Res mappings for positional record types use <c>MapWith()</c> to bypass <c>RecordTypeAdapter.CreateBlockExpression</c>, which throws "Collection was modified"
    /// during eager <c>Compile()</c> when sub-mapping compilation adds entries to Mapster's internal list while that list is being enumerated (a Mapster bug). Req→entity mappings are
    /// unaffected and use the normal fluent API since they target mutable class types.
    /// </remarks>
    public static TypeAdapterConfig ConfigureJobMappings(this TypeAdapterConfig config)
    {
        config.NewConfig<JobDefinitionReq, JobDefinition>()
            .Map(to => to.JobParameters, from => from.CreateParameters)
            .Map(to => to.JobSchedules, from => from.CreateSchedules)
            .Map(to => to.JobTriggerJobDefinitions, from => from.CreateTriggers);

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

        config.NewConfig<JobTriggerParameter, JobTriggerParameterRes>()
            .MapWith(src => new(src.Id, src.JobTriggerId, src.Key, Enum.Parse<JobParameterType>(src.Type), src.Value, src.Description, null, src.Enabled));

        config.NewConfig<JobTrigger, JobTriggerRes>()
            .MapWith(src => new(
                src.Id, src.TriggersJobDefinitionId, src.TriggerJobResultKey, Enum.Parse<ComparisonOperatorEnum>(src.TriggerComparator), src.TriggerJobResultValue, src.Description,
                src.Enabled, null, // JobDefinition — omitted to break circular ref
                src.JobTriggerParameters.Select(p => new JobTriggerParameterRes(
                        p.Id, p.JobTriggerId, p.Key, Enum.Parse<JobParameterType>(p.Type), p.Value, p.Description, null, p.Enabled))
                    .ToList(), null)); // TriggersJobDefinition — omitted to break circular ref

        config.NewConfig<JobScheduleParameter, JobScheduleParameterRes>()
            .MapWith(src => new(src.Id, src.JobScheduleId, src.Key, Enum.Parse<JobParameterType>(src.Type), src.Value, src.Description, null, src.Enabled));

        config.NewConfig<JobSchedule, JobScheduleRes>()
            .MapWith(src => new(
                src.Id, src.JobDefinitionId, Enum.Parse<MonthFlags>(src.MonthFlags), Enum.Parse<DayFlags>(src.DayFlags), Enum.Parse<ScheduleType>(src.Type),
                (src.Times ?? new List<string>()).Select(TimeOnly.Parse).ToList(), src.StartTime != null ? TimeOnly.Parse(src.StartTime) : null,
                src.EndTime != null ? TimeOnly.Parse(src.EndTime) : null, src.IntervalMinutes, src.Description, src.Enabled,
                src.JobScheduleParameters.Select(p => new JobScheduleParameterRes(
                        p.Id, p.JobScheduleId, p.Key, Enum.Parse<JobParameterType>(p.Type), p.Value, p.Description, null, p.Enabled))
                    .ToList(), src.CronExpression));

        config.NewConfig<JobParameter, JobParameterRes>()
            .MapWith(src => new(
                src.Id, src.JobDefinitionId, src.Key, src.Description, Enum.Parse<JobParameterType>(src.Type), src.Value, src.EncryptedValue, src.AllowMultiple, true, src.Required,
                src.ValidationRegex, src.MinLength, src.MaxLength, src.AllowedValues));

        config.NewConfig<JobParallelRestriction, JobParallelRestrictionRes>()
            .MapWith(src => new(
                src.Id, src.BaseJobDefinitionId, src.OtherJobDefinitionId, src.Description, src.Enabled, null)); // OtherJobDefinition — omitted to break circular ref

        config.NewConfig<JobDefinition, JobDefinitionRes>()
            .MapWith(src => new(
                src.Id, src.Name, src.Description, src.Type, src.WorkerType, src.Enabled,
                src.JobParameters.Select(p => new JobParameterRes(
                        p.Id, p.JobDefinitionId, p.Key, p.Description, Enum.Parse<JobParameterType>(p.Type), p.Value, p.EncryptedValue, p.AllowMultiple, true, p.Required,
                        p.ValidationRegex, p.MinLength, p.MaxLength, p.AllowedValues))
                    .ToList(),
                src.JobSchedules.Select(s => new JobScheduleRes(
                        s.Id, s.JobDefinitionId, Enum.Parse<MonthFlags>(s.MonthFlags), Enum.Parse<DayFlags>(s.DayFlags), Enum.Parse<ScheduleType>(s.Type),
                        (s.Times ?? new List<string>()).Select(TimeOnly.Parse).ToList(), s.StartTime != null ? TimeOnly.Parse(s.StartTime) : null,
                        s.EndTime != null ? TimeOnly.Parse(s.EndTime) : null, s.IntervalMinutes, s.Description, s.Enabled,
                        s.JobScheduleParameters.Select(p => new JobScheduleParameterRes(
                                p.Id, p.JobScheduleId, p.Key, Enum.Parse<JobParameterType>(p.Type), p.Value, p.Description, null, p.Enabled))
                            .ToList(), s.CronExpression))
                    .ToList(),
                src.JobTriggerJobDefinitions.Select(t => new JobTriggerRes(
                        t.Id, t.TriggersJobDefinitionId, t.TriggerJobResultKey, Enum.Parse<ComparisonOperatorEnum>(t.TriggerComparator), t.TriggerJobResultValue, t.Description,
                        t.Enabled, null,
                        t.JobTriggerParameters.Select(p => new JobTriggerParameterRes(
                                p.Id, p.JobTriggerId, p.Key, Enum.Parse<JobParameterType>(p.Type), p.Value, p.Description, null, p.Enabled))
                            .ToList(), null))
                    .ToList(),
                src.JobParallelRestrictionBaseJobDefinitions
                    .Select(r => new JobParallelRestrictionRes(r.Id, r.BaseJobDefinitionId, r.OtherJobDefinitionId, r.Description, r.Enabled, null))
                    .ToList(), src.MaxRetryCount, src.RetryBackoffSeconds, src.TimeoutMinutes, src.MaxConcurrentRuns, src.CircuitBreakerThreshold, src.CircuitBreakerResetMinutes,
                src.CircuitBreakerTrippedAt));

        config.NewConfig<JobRun, JobRunRes>()
            .MapWith(src => new() {
                Id = src.Id,
                State = src.State,
                Result = src.Result,
                CreatedTimestamp = src.CreatedTimestamp,
                StartedTimestamp = src.StartedTimestamp,
                FinishedTimestamp = src.FinishedTimestamp,
                AllowTriggers = src.AllowTriggers,
                JobDefinitionId = src.JobDefinitionId,
                // Map when the navigation is loaded; JobDefinitionRes has no JobRuns collection so there is no circular reference.
                JobDefinition = src.JobDefinition == null ? null : src.JobDefinition.Adapt<JobDefinitionRes>(config),
                JobScheduleId = src.JobScheduleId,
                JobSchedule = null, // break circular ref (JobSchedule.JobRuns → JobRun → JobSchedule)
                JobTriggerId = src.JobTriggerId,
                JobTrigger = null, // break circular ref
                ReRanFromJobRun = null, // self-referential — break
                ScheduledSlotUtc = src.ScheduledSlotUtc,
                RetryAttempt = src.RetryAttempt,
                LastHeartbeatUtc = src.LastHeartbeatUtc,
                JobRunParameters =
                    src.JobRunParameters.Select(p => new JobRunParameterRes(
                            p.Id, p.JobRunId, p.Key, Enum.Parse<JobParameterType>(p.Type), p.Value, p.Description, p.EncryptedValue, false))
                        .ToList(),
                JobRunResults = src.JobRunResults.Select(r => new JobRunResultRes(r.Id, r.JobRunId, r.Key, Enum.Parse<JobParameterType>(r.Type), r.Value)).ToList(),
                JobRunLogs = src.JobRunLogs.Select(l => new JobRunLogRes(l.Id, l.JobRunId, Enum.Parse<JobLogLevel>(l.Level), l.Message, l.Context, l.StackTrace, l.Timestamp))
                    .ToList()
            });

        config.NewConfig<JobRunParameter, JobRunParameterRes>()
            .MapWith(src => new(src.Id, src.JobRunId, src.Key, Enum.Parse<JobParameterType>(src.Type), src.Value, src.Description, src.EncryptedValue, false));

        config.NewConfig<JobRunResult, JobRunResultRes>().MapWith(src => new(src.Id, src.JobRunId, src.Key, Enum.Parse<JobParameterType>(src.Type), src.Value));
        config.NewConfig<JobRunLog, JobRunLogRes>()
            .MapWith(src => new(src.Id, src.JobRunId, Enum.Parse<JobLogLevel>(src.Level), src.Message, src.Context, src.StackTrace, src.Timestamp));

        return config;
    }
}