using Lyo.Api.Export;
using Lyo.Api.Export.Csv;
using Lyo.Api.Export.Xlsx;
using Lyo.Api.Middleware;
using Lyo.Authentication;
using Lyo.Authentication.AspNetCore;
using Lyo.Authentication.AspNetCore.Endpoints;
using Lyo.Authentication.Google;
using Lyo.Authentication.OpenIdConnect;
using Lyo.Authentication.OpenIdConnect.Endpoints;
using Lyo.Authentication.Postgres;
using Lyo.Comic.Api;
using Lyo.Comic.Postgres.Database;
using Lyo.Common;
using Lyo.Csv;
using Lyo.KeyStore;
using Lyo.Xlsx;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(i => i.ClearProviders()
    .AddSimpleConsole(c => {
        c.SingleLine = true;
        c.UseUtcTimestamp = true;
    })); //logging
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
builder.Services.AddOpenApi();
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddComicApi(builder.Configuration);
builder.Services.AddLyoApiExport<ComicDbContext>();
builder.Services.AddCsvExport();
builder.Services.AddXlsxExport();

builder.Services.AddLocalKeyStore();
builder.Services.AddLyoAuthentication(builder.Configuration);
builder.Services.AddPostgresAuthenticationStoresFromConfiguration(builder.Configuration);
builder.Services.AddLyoApiTokenAuthentication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddLyoOpenIdConnect(builder.Configuration);
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetSection("GoogleAuth")["ClientId"]))
    builder.Services.AddGoogleProviderFromConfiguration(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<LoggingMiddleware>();
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Lyo.Comic.Api" })).AllowAnonymous().WithTags("Health");
app.MapLyoJwks();
app.MapLyoAuthEndpoints();
app.MapLyoTokenManagementEndpoints();
app.MapComicApi();
app.BuildComicApiEndpoints();
app.Run();
