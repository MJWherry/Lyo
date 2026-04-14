using Lyo.Api.ApiEndpoint;
using Lyo.Api.Mapping;
using Lyo.Cache;
using Lyo.Csv;
using Lyo.Formatter;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Xlsx;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;

namespace Lyo.Api.Tests;

public class ApiEndpointBuilderTests
{
    [Fact]
    public void ApiEndpointBuilder_WithCrud_BuildsWithoutThrowing()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLocalCache();
        builder.Services.AddLyoQueryServices();
        builder.Services.AddPostgresJobManagement(
            new PostgresJobOptions { ConnectionString = "Host=localhost;Database=test;Username=test;Password=test", EnableAutoMigrations = false });

        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        builder.Services.AddSingleton(config);
        builder.Services.AddScoped<IMapper, ServiceMapper>();
        builder.Services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        var app = builder.Build();
        var exception = Record.Exception(() => app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
            .AllowAnonymous()
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = Guid.NewGuid())
            .WithCreateBulk(ctx => ctx.Entity.Id = Guid.NewGuid())
            .WithUpdate()
            .WithUpdateBulk()
            .WithPatch()
            .WithPatchBulk()
            .WithUpsert(beforeCreate: ctx => ctx.Entity.Id = Guid.NewGuid())
            .WithUpsertBulk(beforeCreate: ctx => ctx.Entity.Id = Guid.NewGuid())
            .WithDelete()
            .WithDeleteBulk()
            .Build());

        Assert.Null(exception);
    }

    [Fact]
    public void ApiEndpointBuilder_WithCrudAndBulk_BuildsWithoutThrowing()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLocalCache();
        builder.Services.AddLyoQueryServices();
        builder.Services.AddPostgresJobManagement(
            new PostgresJobOptions { ConnectionString = "Host=localhost;Database=test;Username=test;Password=test", EnableAutoMigrations = false });

        builder.Services.AddCsvService();
        builder.Services.AddXlsxService();
        builder.Services.AddFormatterService();
        builder.Services.WithExportService<JobContext>();
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        builder.Services.AddSingleton(config);
        builder.Services.AddScoped<IMapper, ServiceMapper>();
        builder.Services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        var app = builder.Build();
        var exception = Record.Exception(()
            => app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
                .AllowAnonymous()
                .WithCrud(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, new())
                .Build());

        Assert.Null(exception);
    }

    [Fact]
    public void ApiEndpointBuilder_WithCrudAndFeatureFlags_BuildsWithoutThrowing()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLocalCache();
        builder.Services.AddLyoQueryServices();
        builder.Services.AddPostgresJobManagement(
            new PostgresJobOptions { ConnectionString = "Host=localhost;Database=test;Username=test;Password=test", EnableAutoMigrations = false });

        builder.Services.AddCsvService();
        builder.Services.AddXlsxService();
        builder.Services.AddFormatterService();
        builder.Services.WithExportService<JobContext>();
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        builder.Services.AddSingleton(config);
        builder.Services.AddScoped<IMapper, ServiceMapper>();
        builder.Services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        var app = builder.Build();
        var exception = Record.Exception(()
            => app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
                .AllowAnonymous()
                .WithCrud(ApiFeatureFlag.All, new() { BeforeCreate = ctx => ctx.Entity.Id = Guid.NewGuid() })
                .Build());

        Assert.Null(exception);
    }

    [Fact]
    public void ApiEndpointBuilder_WithReadOnly_BuildsWithoutThrowing()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLocalCache();
        builder.Services.AddLyoQueryServices();
        builder.Services.AddPostgresJobManagement(
            new PostgresJobOptions { ConnectionString = "Host=localhost;Database=test;Username=test;Password=test", EnableAutoMigrations = false });

        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        builder.Services.AddSingleton(config);
        builder.Services.AddScoped<IMapper, ServiceMapper>();
        builder.Services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        var app = builder.Build();
        var exception = Record.Exception(()
            => app.CreateReadOnlyBuilder<JobContext, JobDefinition, JobDefinitionRes>("/api/Job/Definition", "Job").AllowAnonymous().WithReadOnlyEndpoints().Build());

        Assert.Null(exception);
    }

    [Fact]
    public void ApiEndpointBuilder_WithMetadataFeatureFlag_BuildsWithoutThrowing()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLocalCache();
        builder.Services.AddLyoQueryServices();
        builder.Services.AddPostgresJobManagement(
            new PostgresJobOptions { ConnectionString = "Host=localhost;Database=test;Username=test;Password=test", EnableAutoMigrations = false });

        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        builder.Services.AddSingleton(config);
        builder.Services.AddScoped<IMapper, ServiceMapper>();
        builder.Services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        var app = builder.Build();
        var exception = Record.Exception(()
            => app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
                .AllowAnonymous()
                .WithCrud(ApiFeatureFlag.ReadOnly | ApiFeatureFlag.Metadata, new() { Metadata = new() { IncludeEntityMetadata = true } })
                .Build());

        Assert.Null(exception);
    }
}