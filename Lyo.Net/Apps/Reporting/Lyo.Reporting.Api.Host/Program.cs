using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Middleware;
using Lyo.Reporting.Api;
using Lyo.Cache;
using Lyo.Common.Json;
using Lyo.IO.Temp;
using Lyo.Reporting.Postgres;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddLyoApiCompression();
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
builder.Services.AddLocalCache();
builder.Services.AddLyoQueryServices();
builder.Services.AddIOTempService();
builder.Services.AddReportingApiFromConfiguration(builder.Configuration);
builder.Services.AddReportingMaintenanceWorker();
var application = builder.Build();
application.UseMiddleware<LoggingMiddleware>();
application.UseLyoApiCompression();
if (application.Environment.IsDevelopment()) {
    application.MapOpenApi();
    application.MapScalarApiReference();
}

application.BuildReportingGroup(ReportingApiOptions.WithAuth(EndpointAuth.Anonymous()));
application.Run();
