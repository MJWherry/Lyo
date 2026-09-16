using Lyo.Api;
using Lyo.Api.FileStorage;
using Lyo.Api.Middleware;
using Lyo.Cache;
using Lyo.Json;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.HomeInventory.Api;
using Lyo.HomeInventory.FileStorage;
using Lyo.HomeInventory.Postgres;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddLyoApiCompression();
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
builder.Services.AddLocalCache();
builder.Services.AddLyoQueryServices();
builder.Services.AddHomeInventoryApiFromConfiguration(builder.Configuration);
var inventory = new PostgresHomeInventoryOptions();
builder.Configuration.GetSection(PostgresHomeInventoryOptions.SectionName).Bind(inventory);
builder.Services.AddPostgresFileMetadataStoreKeyed("FileStorage")
    .ConfigurePostgresFileStore(o => {
        o.ConnectionString = inventory.ConnectionString;
        o.EnableAutoMigrations = inventory.EnableAutoMigrations;
    })
    .Build();
builder.Services.AddLyoCrudServices<FileMetadataStoreDbContext>();
var diskRoot = builder.Configuration["DiskFileStorage:RootDirectoryPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "filestorage");
builder.Services.AddFileStorageServiceKeyed(
    "FileStorage",
    o => o.RootDirectoryPath = diskRoot,
    sp => sp.GetRequiredKeyedService<IFileMetadataStore>("FileStorage"),
    "FileStorage");
builder.Services.AddScoped<IFileStorageService>(sp => sp.GetRequiredKeyedService<IFileStorageService>("FileStorage"));
builder.Services.AddHomeInventoryFileStorage();
var application = builder.Build();
application.UseMiddleware<LoggingMiddleware>();
application.UseLyoApiCompression();
if (application.Environment.IsDevelopment()) {
    application.MapOpenApi();
    application.MapScalarApiReference();
}

application.BuildHomeInventoryGroup();
application.BuildFileStorageApi(
    new() {
        Route = "FileStorage",
        ServiceKey = "FileStorage",
        FileMetadataRoute = "FileStorage/FileMetadata",
        DirectUploadPath = ""
    });
application.Run();
