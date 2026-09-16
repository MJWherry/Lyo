using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Api.Export;
using Lyo.Api.Export.Csv;
using Lyo.Api.Export.Toml;
using Lyo.Api.Export.Xlsx;
using Lyo.Api.Export.Xml;
using Lyo.Api.Export.Yaml;
using Lyo.Cache;
using Lyo.Json;
using Lyo.Csv;
using Lyo.Formatter;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Xlsx;
using Lyo.Xml;
using Lyo.Yaml;
using Lyo.Toml;
using Lyo.Common.Core.Identifiers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddSingleton<IYamlService>(_ => new YamlService());
builder.Services.AddSingleton<IXmlService>(_ => new XmlService());
builder.Services.AddSingleton<ITomlService>(_ => new TomlService());
builder.Services.AddFormatterService();
builder.Services.AddLyoApiCompression();
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
builder.Services.AddLocalCache();
builder.Services.AddLyoQueryServices();
builder.Services.AddPostgresJobManagementFromConfiguration(builder.Configuration);
builder.Services.AddLyoApiExport<JobContext>();
builder.Services.AddCsvExport();
builder.Services.AddXlsxExport();
builder.Services.AddYamlExport();
builder.Services.AddXmlExport();
builder.Services.AddTomlExport();
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseMiddleware<Lyo.Api.Middleware.LoggingMiddleware>();
app.UseCors();
app.UseLyoApiCompression();
app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
    .AllowAnonymous()
    .WithMetadata(new MetadataConfiguration<JobContext, JobDefinition> { IncludeEntityMetadata = true })
    .WithQuery()
    .WithGet(ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforeGet]", ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterGet]")
    .WithCreate(ctx => ctx.Entity.Id = LyoGuid.CreateV7(), ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterCreate]")
    .WithCreateBulk(ctx => ctx.Entity.Id = LyoGuid.CreateV7(), ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterCreateBulk]")
    .WithUpdate(
        ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforeUpdate]", ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterUpdate]")
    .WithUpdateBulk(
        ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforeUpdateBulk]",
        ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterUpdateBulk]")
    .WithPatch(ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforePatch]", ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterPatch]")
    .WithPatchBulk(
        ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [beforePatchBulk]", ctx => ctx.Entity.Description = (ctx.Entity.Description ?? "") + " [afterPatchBulk]")
    .WithUpsert(beforeCreate: ctx => ctx.Entity.Id = LyoGuid.CreateV7())
    .WithUpsertBulk(beforeCreate: ctx => ctx.Entity.Id = LyoGuid.CreateV7())
    .WithDelete()
    .WithDeleteBulk(_ => { }, _ => { })
    .Build();

app.MapDynamicCrudEndpoints<JobContext>(c => c.AllowAnonymous()
    .WithDefaults(d => {
        d.BaseRoute = "api/Job";
        d.Features = ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance;
        // Match static MapPost create hooks. Dynamic CRUD has no per-entity WithCreate unless registered. Set the Guid PK when the client omits id.
        d.BeforeCreate = ctx => {
            if (ctx.Entity is JobDefinition jd && jd.Id == Guid.Empty)
                jd.Id = LyoGuid.CreateV7();
        };
    })
    .IncludeOnly<JobDefinition, JobRun>());

app.Run();

namespace Lyo.Api.Tests.Host
{
    /// <summary>Entry-point type used by WebApplicationFactory.</summary>
    public class Program;
}