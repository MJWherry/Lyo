using System.IO.Compression;
using System.Text.Json.Serialization;
using Lyo.Api;
using Lyo.Cache;
using Lyo.Compression;
using Lyo.Config.Postgres;
using Lyo.Csv;
using Lyo.DateAndTime.Json;
using Lyo.Discord.Postgres;
using Lyo.Discord.Postgres.Database;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.S3;
using Lyo.Formatter;
using Lyo.Keystore.Aws;
using Lyo.People.Postgres;
using Lyo.People.Postgres.Database;
using Lyo.Sms.Twilio.Postgres;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.TestApi;
using Lyo.TestApi.FileStorageWorkbench;
using Lyo.Xlsx;
using Microsoft.AspNetCore.ResponseCompression;
using Scalar.AspNetCore;
using Constants = Lyo.TestApi.Constants;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = long.MaxValue);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => {
    options.MultipartBodyLengthLimit = long.MaxValue;
});
builder.Services.AddOpenApi();
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
builder.Services.AddMetrics();
builder.Services.AddFormatterService();
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddCompressionService();
builder.Services.AddLocalCacheFromConfiguration(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new DateOnlyModelConverter());
    options.SerializerOptions.Converters.Add(new TimeOnlyModelConverter());
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.ConfigureMapster();
var connStr = builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Port=5437;Database=postgres;Username=root_remote;Password=password";
builder.Services.AddPeopleDbContextFactory(new PostgresPeopleOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddTwilioSmsDbContextFactory(new PostgresTwilioSmsOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddPostgresDiscord(new PostgresDiscordOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddPostgresConfigStore(new PostgresConfigOptions { ConnectionString = connStr, EnableAutoMigrations = true });
builder.Services.AddDiscordGuildSettingsInfrastructure();
builder.Services.AddLyoQueryServices();
builder.Services.AddLyoCrudServices<PeopleDbContext>();
builder.Services.WithExportService<PeopleDbContext>();
builder.Services.WithExportService<DiscordDbContext>();
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

builder.Services.AddS3FileStorageServiceKeyed(Constants.FileStorageWorkbench.ServiceKey)
    .UseFileMetadataStore(Constants.FileStorageWorkbench.MetadataKey)
    .UseEncryptionService(Constants.FileStorageWorkbench.ServiceKey)
    .ConfigureS3FileStorage()
    .Build(builder.Configuration);

builder.Services.AddFileOperationContextAccessor();
builder.Services.AddPostgresFileAuditSink();
builder.Services.AddScoped<IFileAuditEventHandler, FileMetadataQueryCacheInvalidationHandler>();
var app = builder.Build();
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseResponseCompression();
app.UseRequestDecompression();
app.SetupCourtCanaryEndpoints();
app.Run();