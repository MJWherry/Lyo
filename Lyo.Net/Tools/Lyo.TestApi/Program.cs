using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Lyo.Api;
using Lyo.Api.Authentication;
using Lyo.Query.Postgres;
using Lyo.Api.Export;
using Lyo.Api.Export.Csv;
using Lyo.Api.Export.Toml;
using Lyo.Api.Export.Xlsx;
using Lyo.Api.Export.Xml;
using Lyo.Api.Export.Yaml;
using Lyo.Reporting.Api;
using Lyo.Api.Services.Crud.Read;
using Lyo.Authentication;
using Lyo.Authentication.AspNetCore;
using Lyo.Authentication.AspNetCore.Endpoints;
using Lyo.Authentication.Google;
using Lyo.Authentication.Keycloak;
using Lyo.Authentication.OpenIdConnect;
using Lyo.Authentication.Sso.Endpoints;
using Lyo.Authentication.Postgres;
using Lyo.Cache;
using Lyo.FeatureFlags;
using Lyo.FeatureFlags.AspNetCore;
using Lyo.Book.Postgres;
using Lyo.Book.Postgres.Database;
using Lyo.Music.Postgres;
using Lyo.Music.Postgres.Database;
using Lyo.Audiobook.Postgres;
using Lyo.Audiobook.Postgres.Database;
using Lyo.Podcast.Postgres;
using Lyo.Podcast.Postgres.Database;
using Lyo.Video.Postgres;
using Lyo.Video.Postgres.Database;
using Lyo.Google.Youtube.Client;
using Lyo.Comic.Api;
using Lyo.Comic.Postgres;
using Lyo.Algorithms.Api;
using Lyo.Algorithms.Postgres;
using Lyo.Story.Api;
using Lyo.Story.Postgres;
using Lyo.Leaderboard.Api;
using Lyo.HomeInventory.Api;
using Lyo.HomeInventory.FileStorage;
using Lyo.HomeInventory.Postgres;
using Lyo.Recipe.Api;
using Lyo.Recipe.FileStorage;
using Lyo.Recipe.Postgres;
using Lyo.Ledger.Api;
using Lyo.Ledger.Postgres;
using Lyo.Ledger.Postgres.Database;
using Lyo.Billing;
using Lyo.Billing.Api;
using Lyo.Billing.Postgres;
using Lyo.Billing.Postgres.Database;
using Lyo.Json;
using Lyo.Compression;
using Lyo.Config;
using Lyo.Config.Api;
using Lyo.Config.Postgres;
using Lyo.Csv;
using Lyo.DateAndTime.Json;
using Lyo.Discord.Postgres;
using Lyo.Discord.Postgres.Database;
using Lyo.Encryption.Extensions;
using Lyo.Endato.Postgres;
using Lyo.Endato.Postgres.Database;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.S3;
using Lyo.Formatter;
using Lyo.IO.Temp;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Drift.Postgres;
using Lyo.KeyStore;
using Lyo.KeyStore.Aws;
using Lyo.Lock;
using Lyo.Lock.Redis;
using Lyo.Metrics.OpenTelemetry;
using Lyo.MessageQueue.RabbitMq;
using Lyo.People.Postgres;
using Lyo.People.Postgres.Database;
using Lyo.Reporting.Business.Example;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Web;
using Lyo.Sms.Twilio.Postgres;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.SystemInformation;
using Lyo.TestApi;
using Lyo.TestApi.FileStorageWorkbench;
using Lyo.Web.Automation;
using Lyo.Web.Automation.Api;
using Lyo.Web.Automation.FileStorage;
using Lyo.Web.Automation.Playwright.Service;
using Lyo.Web.Automation.Postgres;
using Lyo.Web.WebRenderer;
using Lyo.Xlsx;
using Lyo.Xml;
using Lyo.Yaml;
using Lyo.Toml;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using Scalar.AspNetCore;
using Constants = Lyo.TestApi.Constants;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(i => i.ClearProviders()
    .AddSimpleConsole(c => {
        c.SingleLine = true;
        c.UseUtcTimestamp = true;
    })); //logging

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = long.MaxValue);
builder.Services.Configure<FormOptions>(options => {
    options.MultipartBodyLengthLimit = long.MaxValue;
});

builder.Services.AddOpenApi();
builder.Services.AddLyoApiCompression();
builder.Services.AddLyoMetricsWithOpenTelemetryFromConfiguration(builder.Configuration);
builder.Services.AddMetrics();
builder.Services.AddFormatterService();
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddSingleton<IYamlService>(_ => new YamlService());
builder.Services.AddSingleton<IXmlService>(_ => new XmlService());
builder.Services.AddSingleton<ITomlService>(_ => new TomlService());
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
var connStr = builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Port=5432;Database=postgres;Username=user;Password=password";
builder.Services.SetupRabbitMqServiceFromConfiguration(builder.Configuration, []);
builder.Services.AddMqJobEventPublisherFromConfiguration(builder.Configuration);
builder.Services.AddPostgresJobManagement(opts => {
    opts.ConnectionString = connStr;
    opts.EnableAutoMigrations = true;
});
builder.Services.AddPostgresDriftManagement(opts => {
    opts.ConnectionString = connStr;
    opts.EnableAutoMigrations = true;
});
builder.Services.AddDriftRetentionService();

builder.Services.AddJobMaintenanceService();
builder.Services.AddLyoCrudServices<JobContext>();
builder.Services.AddLyoApiAuthentication();
builder.Services.AddScoped<JobService>();
builder.Services.AddHttpContextAccessor();
// Enable this line to host the built-in cron/interval scheduler here:
// builder.Services.AddJobScheduler();
builder.Services.AddIOTempService();
builder.Services.AddReportingApi(opts => {
    opts.ConnectionString = connStr;
    opts.EnableAutoMigrations = true;
    // This workbench posts composition JSON directly to Generate; the packaged default rejects that.
    opts.AllowAdHocGeneration = true;
});
builder.Services.AddWebRendererServiceFromConfiguration(builder.Configuration);
builder.Services.AddPlaywrightBrowserService(options => options.Headless = true);
builder.Services.AddPlaywrightBrowserSessionFactory();
builder.Services.AddWebAutomationPlanRunner();
builder.Services.AddAutomationApi();
builder.Services.AddAutomationPlansPostgres(new PostgresAutomationPlanOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddAutomationPlanFileStorage(sp => sp.GetRequiredKeyedService<IFileStorageService>(Constants.FileStorageWorkbench.ServiceKey));
builder.Services.AddReportingWebRenderer();
ReportingBusinessExamples.Register();

builder.Services.AddReportingGenerationHooks(
    new() {
        AfterRenderAsync = async (ctx, ct) => {
            var storage = ctx.Services.GetRequiredKeyedService<IFileStorageService>(Constants.FileStorageWorkbench.ServiceKey);
            var saved = await storage.SaveFileAsync(
                    ctx.StagedFilePath!, ctx.FileName, pathPrefix: ctx.PathPrefix ?? ctx.Request.PathPrefix ?? "reports", contentType: ctx.ContentType, ct: ct)
                .ConfigureAwait(false);

            ctx.OutputFileId = saved.Id;
        },
        // Retention cleanup and generation/definition delete: drop the stored output before the generation row is removed.
        OnCleanupAsync = async (ctx, ct) => {
            if (ctx.OutputFileId is not Guid fileId)
                return;

            var storage = ctx.Services.GetRequiredKeyedService<IFileStorageService>(Constants.FileStorageWorkbench.ServiceKey);
            await storage.DeleteFileAsync(fileId, ct: ct).ConfigureAwait(false);
        }
    });

builder.Services.AddPeopleDbContextFactory(new PostgresPeopleOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddEndatoDbContextFactory(new PostgresEndatoOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddTwilioSmsDbContextFactory(new PostgresTwilioSmsOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddPostgresDiscord(new PostgresDiscordOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddPostgresConfigStore(new PostgresConfigOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.Configure<ConfigApiHostingOptions>(_ => { });
builder.Services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<ConfigApiHostingOptions>>().Value);
builder.Services.AddKeyedLocalKeyStore(
    ConfigQueryRoutes.EncryptionKeyName, ks => {
        var seed = SHA256.HashData(Encoding.UTF8.GetBytes("lyo-config-values-dev-key/v1"));
        ks.AddKey(ConfigQueryRoutes.EncryptionKeyName, "v1", seed);
        ks.SetCurrentVersion(ConfigQueryRoutes.EncryptionKeyName, "v1");
    });
builder.Services.AddEncryptionServiceKeyed(ConfigQueryRoutes.EncryptionKeyName, ConfigQueryRoutes.EncryptionKeyName);
builder.Services.AddConfigValueEncryption(ConfigQueryRoutes.EncryptionKeyName);
builder.Services.AddPostgresBookStore(new PostgresBookOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddLyoCrudServices<BookDbContext>();
builder.Services.AddPostgresMusicStore(new PostgresMusicOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddLyoCrudServices<MusicDbContext>();
builder.Services.AddPostgresVideoStore(new PostgresVideoOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddLyoCrudServices<VideoDbContext>();
builder.Services.AddPostgresAudiobookStore(new PostgresAudiobookOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddLyoCrudServices<AudiobookDbContext>();
builder.Services.AddPostgresPodcastStore(new PostgresPodcastOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddLyoCrudServices<PodcastDbContext>();
var youtube = new GoogleYoutubeClientOptions();
builder.Configuration.GetSection(GoogleYoutubeClientOptions.SectionName).Bind(youtube);
if (!string.IsNullOrWhiteSpace(youtube.ApiKey))
    builder.Services.AddGoogleYoutubeClient(youtube);
builder.Services.AddComicApi(new PostgresComicOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddStoryApi(new PostgresStoryOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddAlgorithmsApi(new PostgresAlgorithmOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddLeaderboardApiForWorkbench(connStr, "http://localhost:5251");
builder.Services.AddHomeInventoryApi(new PostgresHomeInventoryOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddRecipeApi(new PostgresRecipeOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddLedgerApi(new PostgresLedgerOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddBillingApi(new PostgresBillingOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddKeyedLocalKeyStore(
    BillingEncryptionKeys.Fields, ks => {
        var seed = SHA256.HashData(Encoding.UTF8.GetBytes("lyo-billing-fields-dev-key/v1"));
        ks.AddKey(BillingEncryptionKeys.Fields, "v1", seed);
        ks.SetCurrentVersion(BillingEncryptionKeys.Fields, "v1");
    });
builder.Services.AddEncryptionServiceKeyed(BillingEncryptionKeys.Fields, BillingEncryptionKeys.Fields);
builder.Services.AddBillingFieldEncryption(BillingEncryptionKeys.Fields);
builder.Services.AddDiscordGuildSettingsInfrastructure();
builder.Services.AddLyoQueryServices();
builder.Services.AddLyoQueryPostgres();
builder.Services.AddConfigQueryServices();
builder.Services.AddLyoCrudServices<PeopleDbContext>();
builder.Services.AddLyoCrudServices<EndatoDbContext>();
builder.Services.AddLyoApiExport<PeopleDbContext>();
builder.Services.AddLyoApiExport<EndatoDbContext>();
builder.Services.AddLyoApiExport<DiscordDbContext>();
builder.Services.AddLyoApiExport<JobContext>();
builder.Services.AddLyoApiExport<LedgerDbContext>();
builder.Services.AddLyoApiExport<BillingDbContext>();
builder.Services.AddCsvExport();
builder.Services.AddXlsxExport();
builder.Services.AddYamlExport();
builder.Services.AddXmlExport();
builder.Services.AddTomlExport();
builder.Services.AddPostgresSprocService<PeopleDbContext>();
builder.Services.AddLyoCrudServices<TwilioSmsDbContext>();
builder.Services.AddLyoCrudServices<FileMetadataStoreDbContext>();
builder.Services.AddTwoKeyEncryptionFromConfiguration(builder.Configuration, Constants.FileStorageWorkbench.ServiceKey, "AwsKeyStore");
builder.Services.AddPostgresFileMetadataStoreKeyed(Constants.FileStorageWorkbench.MetadataKey)
    .ConfigurePostgresFileStore(options => {
        var section = builder.Configuration.GetSection("PostgresFileMetadataStore");
        options.ConnectionString = section["ConnectionString"] ?? connStr;
        options.EnableAutoMigrations = bool.TryParse(section["EnableAutoMigrations"], out var enableAutoMigrations) ? enableAutoMigrations : true;
    })
    .Build();

builder.Services.AddPostgresFileDownloadAccessService();
var redisConnectionString = builder.Configuration.GetSection("Redis")["ConnectionString"] ?? builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
    builder.Services.AddRedisLock(redisConnectionString);
else
    builder.Services.AddLocalLock();

builder.Services.AddS3FileStorageServiceKeyed(Constants.FileStorageWorkbench.ServiceKey)
    .UseFileMetadataStore(Constants.FileStorageWorkbench.MetadataKey)
    .UseEncryptionService(Constants.FileStorageWorkbench.ServiceKey)
    .ConfigureS3FileStorage()
    .Build(builder.Configuration);
builder.Services.AddFileStorageArchiveServiceKeyed(Constants.FileStorageWorkbench.ServiceKey);
builder.Services.AddScoped<IFileStorageService>(sp => sp.GetRequiredKeyedService<IFileStorageService>(Constants.FileStorageWorkbench.ServiceKey));
builder.Services.AddHomeInventoryFileStorage();
builder.Services.AddRecipeFileStorage();

builder.Services.AddFileOperationContextAccessor();
builder.Services.AddPostgresFileAuditSink();
builder.Services.AddScoped<IFileAuditEventHandler, FileMetadataQueryCacheInvalidationHandler>();
builder.Services.AddLocalKeyStore(ks => {
    var seed = SHA256.HashData("lyo-test-api-dev-jwt-signing-key/v1"u8.ToArray());
    ks.AddKey("lyo-sig", "v1", seed);
    ks.SetCurrentVersion("lyo-sig", "v1");
});

builder.Services.AddLyoAuthentication(builder.Configuration);
builder.Services.AddPostgresAuthenticationStores(o => {
    o.ConnectionString = builder.Configuration.GetSection("PostgresUser")["ConnectionString"] ?? connStr;
    o.EnableAutoMigrations = true;
});

builder.Services.AddLyoApiTokenAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddFeatureFlagsFromConfiguration(builder.Configuration);
builder.Services.AddLyoOpenIdConnect(builder.Configuration);
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetSection("GoogleAuth")["ClientId"]))
    builder.Services.AddGoogleProviderFromConfiguration(builder.Configuration);

if (!string.IsNullOrWhiteSpace(builder.Configuration.GetSection("KeycloakAuth")["ClientId"]))
    builder.Services.AddKeycloakProviderFromConfiguration(builder.Configuration);

var app = builder.Build();
EnsurePgTrgm(connStr);
app.UseMiddleware<Lyo.Api.Middleware.LoggingMiddleware>();
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseLyoApiCompression();
app.UseAuthentication();
app.UseFeatureFlags();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Lyo.TestApi" })).AllowAnonymous().WithTags("Health");
app.MapFeatureFlags();
app.MapFeatureFlagManagement();
app.MapGet("/feature-flags/sample", () => Results.Ok(new { feature = "sample" })).RequireFeature("sample");
app.MapLyoJwks();
app.MapLyoAuthEndpoints();
app.MapLyoTokenManagementEndpoints();
app.SetupEndpoints();
app.Run();

static void EnsurePgTrgm(string connectionString)
{
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS pg_trgm;";
    cmd.ExecuteNonQuery();
}