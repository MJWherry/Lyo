// Template notes: https://aka.ms/new-console-template

using System.Linq.Expressions;
using System.Text.Json;
using Lyo.Api;
using Lyo.Api.Client;
using Lyo.Api.Mapping;
using Lyo.Audit.Postgres;
using Lyo.Audit.Postgres.Database;
using Lyo.Cache.Fusion;
using Lyo.Comic.Postgres;
using Lyo.Comment.Postgres;
using Lyo.Common.Json;
using Lyo.Compression;
using Lyo.Csv;
using Lyo.DateAndTime.Json;
using Lyo.Discord.Bot;
using Lyo.Email;
using Lyo.Email.Postgres;
using Lyo.Endato.Client;
using Lyo.Endato.Postgres;
using Lyo.Espn.Fantasy.Football.Client;
using Lyo.FFmpeg;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileStorage;
using Lyo.FileStorage.S3;
using Lyo.Formatter;
using Lyo.HomeInventory.Postgres;
using Lyo.Http.Client;
using Lyo.Http.Client.Flared;
using Lyo.Http.Client.Plan;
using Lyo.Images.Skia;
using Lyo.IO.Temp;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.KeyStore.Aws;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Metrics;
using Lyo.Pdf;
using Lyo.People.Postgres;
using Lyo.Profanity;
using Lyo.QRCode;
using Lyo.Query.Models.Common;
using Lyo.Reporting.Postgres;
using Lyo.Scheduler;
using Lyo.ShortUrl;
using Lyo.ShortUrl.Postgres;
using Lyo.Sms;
using Lyo.Sms.Models;
using Lyo.Sms.Twilio;
using Lyo.Sms.Twilio.Builders;
using Lyo.Sms.Twilio.Postgres;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.Tag.Postgres;
using Lyo.TestConsole;
using Lyo.Tools;
using Lyo.Translation.Aws;
using Lyo.Tts;
using Lyo.Tts.AwsPolly;
using Lyo.Tts.Typecast;
using Lyo.Typecast.Client;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Playwright.Service;
using Lyo.Web.Automation.Selenium.Service;
using Lyo.Web.WebRenderer;
using Lyo.Xlsx;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedVariable
#pragma warning disable CS8601 // Possible null reference assignment.

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => {
        services.AddLogging(i => i.ClearProviders()
            .AddSimpleConsole(c => {
                c.SingleLine = true;
                c.UseUtcTimestamp = true;
            })); //logging

        //services.AddPostgresComicStoreFromConfiguration(context.Configuration);
        services.AddSeleniumBrowserService();
        services.AddPlaywrightBrowserServiceFromConfiguration(context.Configuration);
        services.AddIOTempService(); //temp file management
        services.AddLyoMetrics(); //metrics
        services.AddCompressionService(); //compression
        services.AddDefaultCompressionService<CompressionService>();
        services.AddLyoDiffServices();
        services.AddPdfServiceFromConfiguration(context.Configuration); //pdf
        services.AddFormatterService(); // formatter
        services.AddCsvService(); // csv + tabular preview
        services.AddXlsxService(); // xlsx + tabular preview
        services.AddSkiaImageServiceFromConfiguration(context.Configuration); // image processing (SkiaSharp)
        services.AddScheduler(o => o.CheckIntervalMs = 1000); //scheduler
        services.AddEndatoClientFromConfiguration(context.Configuration); //endato
        services.AddTypecastClientFromConfiguration(context.Configuration); //typecast
        services.AddTypecastTtsServiceFromConfiguration(context.Configuration); //tts Typecast
        services.AddAwsPollyTtsServiceFromConfiguration(context.Configuration); //tts Polly
        services.AddSingleton<ITtsService>(sp
            => new TypecastTtsAppService(
                sp.GetRequiredService<TypecastTtsService>())); // non-generic TTS for Discord; use AwsPollyTtsAppService + AwsPollyTtsService for Polly instead

        services.AddFlaredHttpClientFromConfiguration(context.Configuration);
        services.AddAwsTranslationServiceFromConfiguration(context.Configuration); //translation
        services.AddProfanityFilterServiceFromConfiguration(context.Configuration); //profanity filter
        services.AddEmailServiceFromConfiguration(context.Configuration); //email
        services.AddTwilioSmsServiceFromConfiguration(context.Configuration); //sms twilio
        services.AddShortUrlFromConfiguration(context.Configuration); //short url
        services.AddQRCodeServiceFromConfiguration(context.Configuration); // qrcode (built-in encoder)
        services.AddWebRendererServiceFromConfiguration(context.Configuration); //web renderer
        services.SetupRabbitMqServiceFromConfiguration(context.Configuration, []); // RabbitMQ setup - using configuration binding
        services.AddFFmpegServicesFromConfiguration(context.Configuration);
        services.AddFantasyFootballClientFromConfiguration(context.Configuration);

        // AWS file storage
        services.AddAwsKeyStoreFromConfiguration(context.Configuration);
        services.AddTwoKeyEncryptionServiceKeyed("two-key-aws", "dev/FileStore");

        // database-backed storage
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
        services.AddComicDbContextFactory(new PostgresComicOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        services.AddPostgresTagStore(new PostgresTagOptions { ConnectionString = connStr, EnableAutoMigrations = true });
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.Default.MaxDepth(8);
        config.Default.Settings.NameMatchingStrategy = NameMatchingStrategy.IgnoreCase;
        // register mappings for concrete types
        config.NewConfig<ConditionClause, ConditionClause>();
        config.NewConfig<GroupClause, GroupClause>();
        // map the abstract base polymorphically
        // Identity-map the abstract base by handing back the source instance.
        // Stops Mapster from constructing the abstract type when Compile() runs.
        config.NewConfig<WhereClause, WhereClause>().ConstructUsing(src => src);
        config.NewConfig<TwilioSmsResult, TwilioSmsLogEntity>()
            .Map(dest => dest.To, src => src.Data != null ? src.Data.To ?? "" : "")
            .Map(dest => dest.From, src => src.Data != null ? src.Data.From : null)
            .Map(dest => dest.Body, src => src.Data != null ? src.Data.Body : null)
            .Map(
                dest => dest.MediaUrlsJson,
                src => src.Data != null && src.Data.MediaUrls.Count > 0 ? JsonSerializer.Serialize(src.Data.MediaUrls.Select(u => u.ToString()).ToList()) : null)
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
            "two-key-local-filestore", o => {
                o.RootDirectoryPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "local-filestorage");
                o.EnableDuplicateDetection = true;
                o.DuplicateStrategy = DuplicateHandlingStrategy.ReturnExisting;
                o.EnableMetrics = true; // Enable metrics collection
            }, _ => new LocalFileMetadataStore(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "local-filestore")), "two-key-aws");

        services.AddTransient<IApiClient>(_ => new ApiClient(serializerOptions: LyoJsonSerializerOptions.Create().AddLyoDateOnlyModelConverters()));
        //services.AddJobScheduler(new() { ApiBaseUrl = "http://localhost:5092/" });
        services.AddFusionCacheFromConfiguration(context.Configuration);
        services.AddLyoQueryServices();
        services.AddLyoDiscordBot<LyoDiscordBot>(context.Configuration);
    })
    .Build();

await host.StartAsync();
Console.ReadLine();