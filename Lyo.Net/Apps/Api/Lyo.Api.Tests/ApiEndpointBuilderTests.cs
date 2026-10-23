using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Api.Export.Csv;
using Lyo.Api.Export.Toml;
using Lyo.Api.Export.Xlsx;
using Lyo.Api.Export.Xml;
using Lyo.Api.Export.Yaml;
using Lyo.Cache;
using Lyo.Common.Core.Identifiers;
using Lyo.Csv;
using Lyo.Formatter;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Toml;
using Lyo.Xlsx;
using Lyo.Xml;
using Lyo.Yaml;
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

        var app = builder.Build();
        var exception = Record.Exception(() => app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
            .AllowAnonymous()
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = LyoGuid.CreateV7())
            .WithCreateBulk(ctx => ctx.Entity.Id = LyoGuid.CreateV7())
            .WithUpdate()
            .WithUpdateBulk()
            .WithPatch()
            .WithPatchBulk()
            .WithUpsert(beforeCreate: ctx => ctx.Entity.Id = LyoGuid.CreateV7())
            .WithUpsertBulk(beforeCreate: ctx => ctx.Entity.Id = LyoGuid.CreateV7())
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
        builder.Services.AddSingleton<IYamlService>(_ => new YamlService());
        builder.Services.AddSingleton<IXmlService>(_ => new XmlService());
        builder.Services.AddSingleton<ITomlService>(_ => new TomlService());
        builder.Services.AddFormatterService();
        builder.Services.AddLyoApiExport<JobContext>();
        builder.Services.AddCsvExport();
        builder.Services.AddXlsxExport();
        builder.Services.AddYamlExport();
        builder.Services.AddXmlExport();
        builder.Services.AddTomlExport();
        var app = builder.Build();
        var exception = Record.Exception(()
            => app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
                .AllowAnonymous()
                .WithCrud(ApiFeatureSet.DefaultCrud, new())
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
        builder.Services.AddSingleton<IYamlService>(_ => new YamlService());
        builder.Services.AddSingleton<IXmlService>(_ => new XmlService());
        builder.Services.AddSingleton<ITomlService>(_ => new TomlService());
        builder.Services.AddFormatterService();
        builder.Services.AddLyoApiExport<JobContext>();
        builder.Services.AddCsvExport();
        builder.Services.AddXlsxExport();
        builder.Services.AddYamlExport();
        builder.Services.AddXmlExport();
        builder.Services.AddTomlExport();
        var app = builder.Build();
        var exception = Record.Exception(()
            => app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
                .AllowAnonymous()
                .WithCrud(ApiFeatureSet.CoreAll, new() { BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateV7() })
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

        var app = builder.Build();
        var exception = Record.Exception(()
            => app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
                .AllowAnonymous()
                .WithCrud(ApiFeatureSet.ReadOnly + ApiFeature.Metadata, new() { Metadata = new() { IncludeEntityMetadata = true } })
                .Build());

        Assert.Null(exception);
    }
}