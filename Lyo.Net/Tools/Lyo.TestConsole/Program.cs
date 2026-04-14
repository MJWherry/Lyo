// See https://aka.ms/new-console-template for more information

using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Api;
using Lyo.Api.Client;
using Lyo.Api.Mapping;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Audit.Postgres;
using Lyo.Audit.Postgres.Database;
using Lyo.Cache.Fusion;
using Lyo.Comment;
using Lyo.Comment.Postgres;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Compression;
using Lyo.Csv;
using Lyo.DateAndTime.Json;
using Lyo.Discord.Bot;
using Lyo.Email;
using Lyo.Email.Postgres;
using Lyo.Endato.Postgres;
using Lyo.Espn.Fantasy.Football;
using Lyo.Ffmpeg;
using Lyo.Ffmpeg.Models;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileStorage;
using Lyo.FileStorage.S3;
using Lyo.Formatter;
using Lyo.HomeInventory.Postgres;
using Lyo.Images.Skia;
using Lyo.IO.Temp;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Scheduler;
using Lyo.Keystore.Aws;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Metrics;
using Lyo.Pdf;
using Lyo.People.Postgres;
using Lyo.Preview;
using Lyo.Profanity;
using Lyo.QRCode;
using Lyo.Query.Models.Common;
using Lyo.Scheduler;
using Lyo.ShortUrl;
using Lyo.ShortUrl.Postgres;
using Lyo.Sms.Models;
using Lyo.Sms.Twilio;
using Lyo.Sms.Twilio.Postgres;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.TestConsole;
using Lyo.Translation.Aws;
using Lyo.Tts;
using Lyo.Tts.AwsPolly;
using Lyo.Tts.Typecast;
using Lyo.Typecast.Client;
using Lyo.Web.Reporting.Postgres;
using Lyo.Web.WebRenderer;
using Lyo.Xlsx;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => {
        services.AddLogging(i => i.ClearProviders()
            .AddSimpleConsole(c => {
                c.SingleLine = true;
                c.UseUtcTimestamp = true;
            })); //logging

        services.AddIOTempService(); //temp file management
        services.AddPreviewService(); //preview HTML, images, etc. in browser
        services.AddLyoMetrics(); //metrics
        services.AddCompressionService(); //compression
        services.AddLyoDiffServices();
        services.AddPdfServiceFromConfiguration(context.Configuration); //pdf
        services.AddFormatterService(); // formatter
        services.AddCsvService(); // csv + tabular preview
        services.AddXlsxService(); // xlsx + tabular preview
        services.AddSkiaImageServiceFromConfiguration(context.Configuration); // image processing (SkiaSharp)
        services.AddScheduler(o => o.CheckIntervalMs = 1000); //scheduler
        services.AddTypecastClientFromConfiguration(context.Configuration); //typecast
        services.AddTypecastTtsServiceFromConfiguration(context.Configuration); //tts Typecast
        services.AddAwsPollyTtsServiceFromConfiguration(context.Configuration); //tts Polly
        services.AddSingleton<ITtsService>(sp
            => new TypecastTtsAppService(
                sp.GetRequiredService<TypecastTtsService>())); // non-generic TTS for Discord; use AwsPollyTtsAppService + AwsPollyTtsService for Polly instead

        services.AddAwsTranslationServiceFromConfiguration(context.Configuration); //translation
        services.AddProfanityFilterServiceFromConfiguration(context.Configuration); //profanity filter
        services.AddEmailServiceFromConfiguration(context.Configuration); //email
        services.AddTwilioSmsServiceFromConfiguration(context.Configuration); //sms twilio
        services.AddShortUrlFromConfiguration(context.Configuration); //short url
        services.AddQRCodeServiceFromConfiguration(context.Configuration); // qrcode (built-in encoder)
        services.AddWebRendererServiceFromConfiguration(context.Configuration); //web renderer
        services.SetupRabbitMqServiceFromConfiguration(context.Configuration, []); // RabbitMQ setup - using configuration binding
        services.AddFfmpegServicesFromConfiguration(context.Configuration);
        services.AddFantasyFootballClientFromConfiguration(context.Configuration);

        //aws filestorage
        services.AddAwsKeyStoreFromConfiguration(context.Configuration);
        services.AddTwoKeyEncryptionServiceKeyed("two-key-aws", "dev/CourtCanary/FileStore");

        //database storage
        var connStr = context.Configuration["ConnectionString"];
        services.AddReportingDbContextFactory(new PostgresReportingOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddEndatoDbContextFactory(new PostgresEndatoOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddShortUrlDbContextFactory(new PostgresShortUrlOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddTwilioSmsDbContextFactory(new PostgresTwilioSmsOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddPostgresAuditRecorder(new PostgresAuditOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddPeopleDbContextFactory(new PostgresPeopleOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddPostgresCommentStore(new PostgresCommentOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddPostgresHomeInventoryStore(new PostgresHomeInventoryOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddLyoCrudServices<TwilioSmsDbContext>();
        services.AddLyoCrudServices<AuditDbContext>();
        services.AddEmailDbContextFactory(new PostgresEmailOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddFileMetadataStoreDbContextFactory(new PostgresFileMetadataStoreOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddLyoCrudServices<JobContext>();
        services.AddScoped<JobService>();
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.Default.MaxDepth(8);
        config.Default.Settings.NameMatchingStrategy = NameMatchingStrategy.IgnoreCase;
        // ensure concrete type mappings exist
        config.NewConfig<ConditionClause, ConditionClause>();
        config.NewConfig<GroupClause, GroupClause>();
        // polymorphic mapping for the abstract base
        // Map the abstract base to itself by returning the source instance.
        // This prevents Mapster from trying to instantiate the abstract type during Compile().
        config.NewConfig<WhereClause, WhereClause>().ConstructUsing(src => src);
        config.NewConfig<TwilioSmsResult, TwilioSmsLogEntity>()
            .Map(dest => dest.To, src => src.Data != null ? src.Data.To ?? "" : "")
            .Map(dest => dest.From, src => src.Data != null ? src.Data.From : null)
            .Map(dest => dest.Body, src => src.Data != null ? src.Data.Body : null)
            .Map(
                dest => dest.MediaUrlsJson,
                src => src.Data != null && src.Data.MediaUrls != null && src.Data.MediaUrls.Count > 0
                    ? JsonSerializer.Serialize(src.Data.MediaUrls.Select(u => u.ToString()).ToList())
                    : null)
            .Map(dest => dest.IsSuccess, src => src.IsSuccess)
            .Map(dest => dest.Message, _ => (string?)null)
            .Map(dest => dest.ErrorMessage, src => src.Errors != null && src.Errors.Count > 0 ? src.Errors[0].Message : null)
            .Map(dest => dest.ElapsedTimeMs, _ => 0L)
            .Map(dest => dest.CreatedTimestamp, src => src.Timestamp)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.DateCreated, src => src.DateCreated)
            .Map(dest => dest.DateSent, src => src.DateSent)
            .Map(dest => dest.DateUpdated, src => src.DateUpdated)
            .Map(dest => dest.NumSegments, src => src.NumSegments)
            .Map(dest => dest.AccountSid, src => src.AccountSid)
            .Map(dest => dest.Price, src => src.Price)
            .Map(dest => dest.PriceUnit, src => src.PriceUnit)
            .Map(dest => dest.ErrorCode, src => src.TwilioErrorCode)
            .Map(dest => dest.Direction, src => src.Direction == Direction.Inbound ? MessageDirection.Inbound : MessageDirection.Outbound)
            .IgnoreNonMapped(true);

        Expression<Func<TwilioSmsLogEntity, TwilioSmsResult>> mapEntityToResult = src => SmsLogMappingHelper.MapToTwilioSmsResult(src);
        config.NewConfig<TwilioSmsLogEntity, TwilioSmsResult>().MapWith(mapEntityToResult);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        services.AddPostgresFileMetadataStoreKeyed("postgres-filemetadatastore").Build();
        services.AddS3FileStorageServiceKeyed("client-files")
            .UseFileMetadataStore("postgres-filemetadatastore")
            .UseEncryptionService("two-key-aws")
            .ConfigureS3FileStorage()
            .Build(context.Configuration);

        services.AddFileStorageServiceKeyed(
            "two-key-local-filestore", config => {
                config.RootDirectoryPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "local-filestorage");
                config.EnableDuplicateDetection = true;
                config.DuplicateStrategy = DuplicateHandlingStrategy.ReturnExisting;
                config.EnableMetrics = true; // Enable metrics collection
            }, _ => new LocalFileMetadataStore(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "local-filestore")), "two-key-aws");

        services.AddTransient<IApiClient>(_ => new ApiClient(
            serializerOptions: new() {
                PropertyNameCaseInsensitive = true, Converters = { new DateOnlyModelConverter(), new TimeOnlyModelConverter(), new JsonStringEnumConverter() }
            }));

        services.AddJobScheduler(new() { ApiBaseUrl = "http://localhost:5092/", TimezoneState = USState.PA });

        services.AddFusionCacheFromConfiguration(context.Configuration);
        services.AddLyoQueryServices();
        services.AddLyoDiscordBot<LyoDiscordBot>(context.Configuration);
    })
    .Build();

await host.StartAsync();
using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;
var logger = sp.GetRequiredService<ILogger<Program>>();

var discordBotOpts = host.Services.GetRequiredService<IOptions<LyoDiscordBotOptions>>().Value;
if (!string.IsNullOrWhiteSpace(discordBotOpts.Token)) {
    using var botCts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => {
        e.Cancel = true;
        botCts.Cancel();
    };

    var bot = host.Services.GetRequiredService<LyoDiscordBot>();
    await bot.RunAsync(botCts.Token).ConfigureAwait(false);
    await host.StopAsync();
    return;
}