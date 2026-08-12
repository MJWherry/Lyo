using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Lyo.Api;
using Lyo.Api.Export;
using Lyo.Api.Export.Csv;
using Lyo.Api.Export.Xlsx;
using Lyo.Api.Reporting;
using Lyo.Api.Services.Crud.Read;
using Lyo.Authentication.AspNetCore;
using Lyo.Authentication.AspNetCore.Endpoints;
using Lyo.Authentication.Google;
using Lyo.Authentication.OpenIdConnect;
using Lyo.Authentication.OpenIdConnect.Endpoints;
using Lyo.Cache;
using Lyo.Common;
using Lyo.Compression;
using Lyo.Config.Api;
using Lyo.Csv;
using Lyo.DateAndTime.Json;
using Lyo.Encryption.Extensions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;
using Lyo.Formatter;
using Lyo.IO.Temp;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.KeyStore;
using Lyo.Lock;
using Lyo.MessageQueue.RabbitMq;
using Lyo.People.Postgres;
using Lyo.People.Postgres.Database;
using Lyo.Portfolio.Api;
using Lyo.Portfolio.Api.FileStorageWorkbench;
using Lyo.Reporting.Postgres;
using Lyo.Xlsx;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(i => i.ClearProviders()
    .AddSimpleConsole(c => {
        c.SingleLine = true;
        c.UseUtcTimestamp = true;
    }));

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = long.MaxValue);
builder.Services.Configure<FormOptions>(options => {
    options.MultipartBodyLengthLimit = long.MaxValue;
});

builder.Services.AddOpenApi();
builder.Services.AddResponseCompression(options => {
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.AddRequestDecompression();
builder.Services.AddMetrics();
builder.Services.AddFormatterService();
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddCompressionService();
builder.Services.AddDefaultCompressionService<CompressionService>();
builder.Services.AddCompressionPolicySelector(builder.Configuration);
builder.Services.AddLocalCacheFromConfiguration(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options => {
    LyoJsonSerializerOptions.ApplyTo(options.SerializerOptions);
    options.SerializerOptions.AddLyoDateOnlyModelConverters();
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.Configure<QueryOptions>(builder.Configuration.GetSection("QueryOptions"));
builder.Services.ConfigureMapster();

var connStr = builder.Configuration.GetConnectionString("Postgres")
              ?? "Host=localhost;Port=5432;Database=lyo;Username=lyo;Password=lyo";

builder.Services.SetupRabbitMqServiceFromConfiguration(builder.Configuration, []);
builder.Services.AddMqJobEventPublisher();
builder.Services.AddPostgresJobManagement(opts => {
    opts.ConnectionString = connStr;
    opts.EnableAutoMigrations = true;
});
builder.Services.AddJobMaintenanceService();
builder.Services.AddLyoCrudServices<JobContext>();
builder.Services.AddScoped<JobService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddIOTempService();
builder.Services.AddReportingApi(opts => {
    opts.ConnectionString = connStr;
    opts.EnableAutoMigrations = true;
});
builder.Services.AddReportingGenerationHooks(
    new() {
        AfterRenderAsync = async (ctx, ct) => {
            var storage = ctx.Services.GetRequiredKeyedService<IFileStorageService>(PortfolioRoutes.FileStorageWorkbench.ServiceKey);
            var saved = await storage.SaveFileAsync(
                    ctx.StagedFilePath!, ctx.FileName, pathPrefix: ctx.PathPrefix ?? ctx.Request.PathPrefix ?? "reports", contentType: ctx.ContentType, ct: ct)
                .ConfigureAwait(false);
            ctx.OutputFileId = saved.Id;
        },
        OnCleanupAsync = async (ctx, ct) => {
            if (ctx.OutputFileId is not Guid fileId)
                return;
            var storage = ctx.Services.GetRequiredKeyedService<IFileStorageService>(PortfolioRoutes.FileStorageWorkbench.ServiceKey);
            await storage.DeleteFileAsync(fileId, ct: ct).ConfigureAwait(false);
        }
    });

builder.Services.AddPeopleDbContextFactory(new PostgresPeopleOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddLyoQueryServices();
builder.Services.AddLyoCrudServices<PeopleDbContext>();
builder.Services.AddLyoApiExport<PeopleDbContext>();
builder.Services.AddLyoApiExport<JobContext>();
builder.Services.AddCsvExport();
builder.Services.AddXlsxExport();
builder.Services.AddPostgresSprocService<PeopleDbContext>();
builder.Services.AddLyoCrudServices<FileMetadataStoreDbContext>();
builder.Services.AddLocalLock();

// Config API (includes Lyo auth + Postgres user stores + scopes)
builder.Services.AddConfigApi(builder.Configuration);

// Local file storage + Postgres metadata (no S3)
var fileKey = PortfolioRoutes.FileStorageWorkbench.ServiceKey;
var metaKey = PortfolioRoutes.FileStorageWorkbench.MetadataKey;
var fileRoot = builder.Configuration["LocalFileStorage:RootDirectoryPath"] ?? "/var/lyo/portfolio-files";
var encKeyId = builder.Configuration["PortfolioFileEncryption:KeyId"] ?? "portfolio-files";
var encSecret = builder.Configuration["PortfolioFileEncryption:KeySecret"] ?? "change-me-in-production";

var fileKeyStore = new LocalKeyStore();
fileKeyStore.AddKeyFromString(encKeyId, "v1", encSecret);
builder.Services.AddKeyedSingleton<IKeyStore>(fileKey, (_, _) => fileKeyStore);
builder.Services.AddKeyedSingleton<LocalKeyStore>(fileKey, (_, _) => fileKeyStore);
builder.Services.AddEncryptionServiceKeyed(fileKey, fileKey);

builder.Services.AddPostgresFileMetadataStoreKeyed(metaKey)
    .ConfigurePostgresFileStore(options => {
        var section = builder.Configuration.GetSection("PostgresFileMetadataStore");
        options.ConnectionString = section["ConnectionString"] ?? connStr;
        options.EnableAutoMigrations = bool.TryParse(section["EnableAutoMigrations"], out var enable) ? enable : true;
    })
    .Build();

builder.Services.AddFileStorageServiceKeyed(
    fileKey,
    opts => opts.RootDirectoryPath = fileRoot,
    provider => {
        var factory = provider.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>();
        var db = factory.CreateDbContext();
        var loggerFactory = provider.GetService<ILoggerFactory>();
        return new PostgresFileMetadataStore(db, loggerFactory);
    },
    fileKey);

builder.Services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
builder.Services.TryAddInMemoryStagedFileUploadStoreIfMissing();
builder.Services.AddKeyedScoped<IMultipartUploadService>(
    fileKey, (sp, _) => new LocalMultipartUploadService(
        sp.GetRequiredKeyedService<LocalFileStorageService>(fileKey),
        sp.GetRequiredService<IMultipartUploadSessionStore>(),
        sp.GetRequiredService<DiskFileStorageOptions>(),
        loggerFactory: sp.GetService<ILoggerFactory>()));
builder.Services.AddKeyedScoped<IStagedFileUploadService>(
    fileKey, (sp, _) => new LocalStagedFileUploadService(
        sp.GetRequiredKeyedService<LocalFileStorageService>(fileKey),
        sp.GetRequiredService<IStagedFileUploadStore>(),
        sp.GetRequiredService<DiskFileStorageOptions>(),
        loggerFactory: sp.GetService<ILoggerFactory>()));

builder.Services.AddPostgresFileDownloadAccessService();
builder.Services.AddFileOperationContextAccessor();
builder.Services.AddPostgresFileAuditSink();
builder.Services.AddScoped<IFileAuditEventHandler, FileMetadataQueryCacheInvalidationHandler>();

// OIDC + Google (AddConfigApi already registered JWT auth + user stores)
builder.Services.AddLyoOpenIdConnect(builder.Configuration);
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetSection("GoogleAuth")["ClientId"]))
    builder.Services.AddGoogleProviderFromConfiguration(builder.Configuration);

builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseResponseCompression();
app.UseRequestDecompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Lyo.Portfolio.Api" }))
    .AllowAnonymous()
    .WithTags("Health");
app.MapLyoJwks();
app.MapLyoAuthEndpoints();
app.MapLyoTokenManagementEndpoints();
app.SetupPortfolioEndpoints();
app.Run();
