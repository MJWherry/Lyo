using System.IO.Compression;
using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Api.Mapping;
using Lyo.Api.Tests.Host;
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
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddFormatterService();
var config = new TypeAdapterConfig();
config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
config.ConfigureJobMappings();
builder.Services.AddResponseCompression(options => {
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options => {
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options => {
    options.Level = CompressionLevel.Fastest;
});

builder.Services.AddRequestDecompression();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNameCaseInsensitive = true);
builder.Services.AddLocalCache();
builder.Services.AddLyoQueryServices();
builder.Services.AddPostgresJobManagementFromConfiguration(builder.Configuration);
builder.Services.WithExportService<JobContext>();
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();
builder.Services.AddScoped<ILyoMapper, MapsterLyoMapper>();
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors();
app.UseResponseCompression();
app.UseRequestDecompression();
app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
    .AllowAnonymous()
    .WithMetadata(new MetadataConfiguration<JobContext, JobDefinition> { IncludeEntityMetadata = true })
    .WithQuery()
    .WithGet(ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforeGet]", ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterGet]")
    .WithCreate(ctx => ctx.Entity.Id = Guid.NewGuid(), ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterCreate]")
    .WithCreateBulk(ctx => ctx.Entity.Id = Guid.NewGuid(), ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterCreateBulk]")
    .WithUpdate(
        ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforeUpdate]", ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterUpdate]")
    .WithUpdateBulk(
        ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforeUpdateBulk]",
        ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterUpdateBulk]")
    .WithPatch(ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforePatch]", ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterPatch]")
    .WithPatchBulk(
        ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforePatchBulk]", ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterPatchBulk]")
    .WithUpsert(beforeCreate: ctx => ctx.Entity.Id = Guid.NewGuid())
    .WithUpsertBulk(beforeCreate: ctx => ctx.Entity.Id = Guid.NewGuid())
    .WithDelete(
        ctx => { /* beforeDelete - entity about to be deleted */
        }, ctx => { /* afterDelete - entity already deleted */
        })
    .WithDeleteBulk(ctx => { }, ctx => { })
    .Build();

app.MapDynamicCrudEndpoints<JobContext>(c => c.WithDefaults(d => {
        d.BaseRoute = "api/Job";
        d.Features = ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate;
    })
    .IncludeOnly<JobDefinition>());

app.Run();

/// <summary>Entry point type for WebApplicationFactory.</summary>
public partial class Program;