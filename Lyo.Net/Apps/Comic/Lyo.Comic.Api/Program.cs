using Lyo.Api;
using Lyo.Comic.Api;
using Lyo.Comic.Postgres.Database;
using Lyo.Csv;
using Lyo.Xlsx;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCsvService();
builder.Services.AddXlsxService();

builder.Services.AddComicApi(builder.Configuration);

builder.Services.WithExportService<ComicDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapComicApi();
app.BuildComicApiEndpoints();
app.Run();
