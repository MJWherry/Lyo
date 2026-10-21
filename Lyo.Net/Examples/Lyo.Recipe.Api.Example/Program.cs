using Lyo.Api;
using Lyo.Api.FileStorage;
using Lyo.Api.Middleware;
using Lyo.Cache;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.Json;
using Lyo.Recipe.Api;
using Lyo.Recipe.FileStorage;
using Lyo.Recipe.Postgres;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddLyoApiCompression();
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
builder.Services.AddLocalCache();
builder.Services.AddLyoQueryServices();
builder.Services.AddRecipeApiFromConfiguration(builder.Configuration);
var recipe = new PostgresRecipeOptions();
builder.Configuration.GetSection(PostgresRecipeOptions.SectionName).Bind(recipe);
builder.Services.AddPostgresFileMetadataStoreKeyed("FileStorage")
    .ConfigurePostgresFileStore(o => {
        o.ConnectionString = recipe.ConnectionString;
        o.EnableAutoMigrations = recipe.EnableAutoMigrations;
    })
    .Build();

builder.Services.AddLyoCrudServices<FileMetadataStoreDbContext>();
var diskRoot = builder.Configuration["DiskFileStorage:RootDirectoryPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "filestorage");
builder.Services.AddFileStorageServiceKeyed("FileStorage", o => o.RootDirectoryPath = diskRoot, sp => sp.GetRequiredKeyedService<IFileMetadataStore>("FileStorage"), "FileStorage");
builder.Services.AddScoped<IFileStorageService>(sp => sp.GetRequiredKeyedService<IFileStorageService>("FileStorage"));
builder.Services.AddRecipeFileStorage();
var application = builder.Build();
application.UseMiddleware<LoggingMiddleware>();
application.UseLyoApiCompression();
if (application.Environment.IsDevelopment()) {
    application.MapOpenApi();
    application.MapScalarApiReference();
}

application.BuildRecipeGroup();
application.BuildFileStorageApi(
    new() {
        Route = "FileStorage",
        ServiceKey = "FileStorage",
        FileMetadataRoute = "FileStorage/FileMetadata",
        DirectUploadPath = ""
    });

application.Run();