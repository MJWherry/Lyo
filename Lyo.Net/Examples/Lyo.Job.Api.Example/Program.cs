using Lyo.Api;
using Lyo.Api.Export;
using Lyo.Api.Export.Csv;
using Lyo.Api.Export.Toml;
using Lyo.Api.Export.Xlsx;
using Lyo.Api.Export.Xml;
using Lyo.Api.Export.Yaml;
using Lyo.Api.Middleware;
using Lyo.Cache;
using Lyo.Csv;
using Lyo.Job.Api;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Json;
using Lyo.Toml;
using Lyo.Xlsx;
using Lyo.Xml;
using Lyo.Yaml;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddLyoApiCompression();
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
builder.Services.AddLocalCache();
builder.Services.AddLyoQueryServices();
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddSingleton<IYamlService>(_ => new YamlService());
builder.Services.AddSingleton<IXmlService>(_ => new XmlService());
builder.Services.AddSingleton<ITomlService>(_ => new TomlService());
builder.Services.AddPostgresJobManagementFromConfiguration(builder.Configuration);
builder.Services.AddLyoApiExport<JobContext>();
builder.Services.AddCsvExport();
builder.Services.AddXlsxExport();
builder.Services.AddYamlExport();
builder.Services.AddXmlExport();
builder.Services.AddTomlExport();
builder.Services.AddJobMaintenanceService();
var application = builder.Build();
application.UseMiddleware<LoggingMiddleware>();
application.UseLyoApiCompression();
if (application.Environment.IsDevelopment()) {
    application.MapOpenApi();
    application.MapScalarApiReference();
}

application.BuildJobGroup();
application.Run();