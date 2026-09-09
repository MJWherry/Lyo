using Blazored.LocalStorage;
using Lyo.Api.Client;
using Lyo.Authentication.Client;
using Lyo.Authentication.Web.Components;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Options;
using Lyo.Authentication.Web.Components.Server;
using Lyo.Barcode.Native;
using Lyo.Cache;
using Lyo.Common.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Compression;
using Lyo.Config.Api.Client;
using Lyo.Csv;
using Lyo.DateAndTime.Json;
using Lyo.Email;
using Lyo.Endato.Client;
using Lyo.FileStorage.Web.Components.Services;
using Lyo.Formatter;
using Lyo.Formatter.Web.Components;
using Lyo.TestGateway;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Services;
using Lyo.TestGateway.Stores;
using Lyo.Diagnostic.Web.Components;
using Lyo.Images;
using Lyo.IO.Temp;
using Lyo.Job.Web.Components;
using Lyo.Drift.Web.Components;
using Lyo.KeyStore;
using Lyo.Lock;
using Lyo.MessageQueue.RabbitMq;
using Lyo.MessageQueue.RabbitMq.Web.Components;
using Lyo.Reporting.Business.Example;
using Lyo.Reporting.Web.Components;
using Lyo.Sms.Web.Components;
using Lyo.Metrics.OpenTelemetry;
using Lyo.Pdf;
using Lyo.Pdf.Web.Components.PdfAnnotator;
using Lyo.Privacy.Web.Components;
using Lyo.Profanity;
using Lyo.Schedule.Web.Components;
using Lyo.QRCode;
using Lyo.Scheduler;
using Lyo.Seed;
using Lyo.Sms.Twilio;
using Lyo.Translation.Aws;
using Lyo.Tts.Typecast;
using Lyo.Typecast.Client;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.Export;
using Lyo.Web.Components.ParamTable;
using Lyo.Web.Host;
using Lyo.Web.WebRenderer;
using Lyo.Xlsx;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(i => i.ClearProviders()
    .AddSimpleConsole(c => {
        c.SingleLine = true;
        c.UseUtcTimestamp = true;
    })); //logging

builder.Services.AddHttpContextAccessor();
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddLyoDataGridExport();
builder.Services.AddLyoMetricsWithOpenTelemetryFromConfiguration(builder.Configuration);
builder.Services.AddScheduler();
builder.Services.AddLocalCacheFromConfiguration(builder.Configuration);
builder.Services.AddLocalLock(options => options.EnableMetrics = true);
builder.Services.AddLocalKeyedSemaphore(options => options.EnableMetrics = true);
builder.Services.AddCompressionService();
builder.Services.AddDefaultCompressionService<CompressionService>();
builder.Services.AddCompressionPolicySelector(builder.Configuration);
builder.Services.AddImageSharpImageServiceFromConfiguration(builder.Configuration);
builder.Services.AddQRCodeServiceFromConfiguration(builder.Configuration);
builder.Services.AddNativeBarcodeServiceFromConfiguration(builder.Configuration);
builder.Services.AddTypecastClientFromConfiguration(builder.Configuration);
builder.Services.AddTypecastTtsServiceFromConfiguration(builder.Configuration);
builder.Services.AddAwsTranslationServiceFromConfiguration(builder.Configuration);
builder.Services.AddProfanityFilterServiceFromConfiguration(builder.Configuration);
builder.Services.AddEndatoClientFromConfiguration(builder.Configuration);
builder.Services.AddEmailServiceFromConfiguration(builder.Configuration);
builder.Services.AddTwilioSmsServiceFromConfiguration(builder.Configuration);
builder.Services.SetupRabbitMqServiceFromConfiguration(builder.Configuration, new());
builder.Services.AddWebRendererServiceFromConfiguration(builder.Configuration);
ReportingBusinessExamples.Register();
builder.Services.AddFileStorageWorkbenchSupport(builder.Configuration);
builder.Services.AddLocalKeyStore();
builder.Services.AddConfigApiStore();
builder.Services.AddLyoSeed();
builder.Services.AddFormatterService();
builder.Services.AddLyoFormatterValueEditor(c => {
    foreach (var (key, value) in CourtFormatterContext.Values)
        c.Add(key, value);
});

// Card layout is the gateway default so that chrome is exercised; the toggle stays on so the table is one click away.
builder.Services.AddLyoParameterEditor(o => {
    o.Layout = LyoParameterLayout.Cards;
    o.AllowLayoutToggle = true;
});
builder.Services.AddSingleton<IIOTempService>(new IOTempService(new() { DirectoryName = "lyo-gateway-uploads", CreateRootDirectoryIfNotExists = true }));
builder.Services.Configure<ApiClientOptions>(builder.Configuration.GetSection(ApiClientOptions.SectionName));
builder.Services.AddTransient(provider => provider.GetRequiredService<IOptions<ApiClientOptions>>().Value);
builder.Services.AddLyoAuthClient(builder.Configuration);
builder.Services.AddLyoAuthBlazorStateProvider();
builder.Services.AddLyoApiClient(httpClientBuilderOverride: clientBuilder => clientBuilder.AddLyoAuthHandler());
builder.Services.AddAuthorization();
builder.Services.AddLyoAuthWebComponents(builder.Configuration);
builder.Services.PostConfigure<LyoAuthWebComponentsOptions>(opts => {
    if (opts.Providers.Count == 0) {
        opts.Providers.Add(new("google", "Sign in with Google", Icons.Material.Filled.AccountCircle));
        opts.Providers.Add(new("keycloak", "Sign in with Keycloak", Icons.Material.Filled.Shield));
    }
});

builder.Services.AddLyoAuthWebComponentsServer();
builder.Services.AddScoped<IAuthPasswordSignIn, GatewayPlaceholderPasswordSignIn>();
builder.Services.AddSingleton(_ => {
    var options = LyoJsonSerializerOptions.Create();
    options.AddLyoDateOnlyModelConverters();
    options.WriteIndented = true;
    return options;
});

builder.Services.AddLyoWebShell();
builder.Services.AddPdfService();
builder.Services.AddPdfAnnotatorInterop();
builder.Services.AddScoped<TestGatewayFileTransformer>();
builder.Services.AddSpriteSheetExportService();
builder.Services.AddLyoJobStatusPalettes();
builder.Services.AddLyoDriftStatusPalettes();
builder.Services.AddLyoReportStatusPalette();
builder.Services.AddLyoSmsStatusPalette();
builder.Services.AddLyoRabbitMqStatusPalette();
builder.Services.AddLyoScheduleWorkbench();
builder.Services.AddLyoDiagnosticWorkbench();
builder.Services.AddLyoPrivacyWorkbench();
builder.Services.AddSingleton<IUserStore, HybridUserStore>();

// Register services on the container.
// Query JSON can grow large when a user pastes a deep SubQuery tree into the editor.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options => {
        // Oversized payloads (for example PDF-annotator iframe HTML arriving through JS interop).
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024;
    });

var app = builder.Build();

// Wire the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", true);
    // HSTS defaults to 30 days. Adjust for production if needed: https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapLyoAuthSignIn();
app.MapLyoAuthHandoffCallback();
app.MapLyoAuthSignOut();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(LyoWorkbenchHost).Assembly);
app.Run();