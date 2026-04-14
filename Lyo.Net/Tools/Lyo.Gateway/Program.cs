using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using Lyo.Api.Client;
using Lyo.Barcode.Native;
using Lyo.Cache;
using Lyo.Common.Records;
using Lyo.Compression;
using Lyo.Csv;
using Lyo.DateAndTime.Json;
using Lyo.Email;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Web.Components.Services;
using Lyo.Gateway.Components;
using Lyo.Gateway.Services;
using Lyo.Gateway.Stores;
using Lyo.IO.Temp;
using Lyo.IO.Temp.Models;
using Lyo.Images;
using Lyo.Lock;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Metrics;
using Lyo.Pdf;
using Lyo.Pdf.Web.Components.PdfAnnotator;
using Lyo.Profanity;
using Lyo.QRCode;
using Lyo.Sms.Twilio;
using Lyo.Translation.Aws;
using Lyo.Tts.Typecast;
using Lyo.Typecast.Client;
using Lyo.Web.Components;
using Lyo.Web.WebRenderer;
using Lyo.Xlsx;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddLyoMetrics();
builder.Services.AddLocalCacheFromConfiguration(builder.Configuration);
builder.Services.AddLocalLock(options => options.EnableMetrics = true);
builder.Services.AddLocalKeyedSemaphore(options => options.EnableMetrics = true);
builder.Services.AddCompressionService();
builder.Services.AddImageSharpImageServiceFromConfiguration(builder.Configuration);
builder.Services.AddQRCodeServiceFromConfiguration(builder.Configuration);
builder.Services.AddNativeBarcodeServiceFromConfiguration(builder.Configuration);
builder.Services.AddTypecastClientFromConfiguration(builder.Configuration);
builder.Services.AddTypecastTtsServiceFromConfiguration(builder.Configuration);
builder.Services.AddAwsTranslationServiceFromConfiguration(builder.Configuration);
builder.Services.AddProfanityFilterServiceFromConfiguration(builder.Configuration);
builder.Services.AddEmailServiceFromConfiguration(builder.Configuration);
builder.Services.AddTwilioSmsServiceFromConfiguration(builder.Configuration);
builder.Services.SetupRabbitMqServiceFromConfiguration(builder.Configuration, new());
builder.Services.AddWebRendererServiceFromConfiguration(builder.Configuration);
builder.Services.AddFileStorageWorkbenchSupport(builder.Configuration);
builder.Services.AddSingleton<IIOTempService>(new IOTempService(new IOTempServiceOptions {
    RootDirectory = Path.Combine(Path.GetTempPath(), "lyo-gateway-uploads"), CreateRootDirectoryIfNotExists = true
}));
builder.Services.Configure<ApiClientOptions>(builder.Configuration.GetSection(ApiClientOptions.SectionName));
builder.Services.AddTransient(provider => provider.GetRequiredService<IOptions<ApiClientOptions>>().Value);
builder.Services.AddLyoApiClient();
builder.Services.AddSingleton(_ => new JsonSerializerOptions {
    WriteIndented = true, PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(), new TimeOnlyModelConverter(), new DateOnlyModelConverter() }
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddPdfService();
builder.Services.AddScoped<IJsInterop, JsInterop>();
builder.Services.AddPdfAnnotatorInterop();
builder.Services.AddScoped<TestGatewayFileTransformer>();
builder.Services.AddSpriteSheetExportService();
builder.Services.AddScoped<ClientStore>();
builder.Services.AddSingleton<IUserStore, HybridUserStore>();
builder.Services.AddMudServices(config => {
    config.PopoverOptions.ModalOverlay = true; // v9 default is false; true restores v8 behavior (menus close when clicking activator)
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
    config.SnackbarConfiguration.ErrorIcon = Icons.Material.Filled.BugReport;
});

// Add services to the container.
// Query JSON payloads can be large when users paste big SubQuery trees in the editor.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options => {
        // Large payloads (e.g. PDF annotator iframe HTML via JS interop).
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// File Storage Workbench download: asks Test API for a time-limited storage URL when safe (plain files → e.g. S3 presigned), redirects the browser there so bytes never cross Gateway; otherwise streams decrypted output from Test API.
app.MapGet(
        $"/{Lyo.Gateway.Models.Constants.FileStorageWorkbench.ProxyDownloadRoute}/{{fileId:guid}}",
        async (
            HttpContext http,
            Guid fileId,
            double? expiresHours,
            IApiClient apiClient,
            IHttpClientFactory httpClientFactory,
            IOptions<FileStorageWorkbenchOptions> fsw,
            IOptions<ApiClientOptions> apiOptions,
            CancellationToken ct) => {
            if (!fsw.Value.UseTestApiServices)
                return Results.BadRequest("File storage workbench is not configured to use Test API services.");

            var baseUrl = apiOptions.Value.BaseUrl?.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl))
                return Results.Problem("ApiClient:BaseUrl is not configured; cannot download via workbench.");

            var prefix = fsw.Value.ApiRoutePrefix.Trim().Trim('/');

            FileStoreResult? metadata;
            try {
                metadata = await apiClient.GetAsAsync<FileStoreResult>($"{prefix}/files/{fileId:D}/metadata", ct: ct).ConfigureAwait(false);
            }
            catch {
                return Results.NotFound();
            }

            if (metadata == null)
                return Results.NotFound();

            // Plain objects: use IFileStorageService time-limited read URL (S3/Azure presigned, etc.) so the browser loads directly from storage.
            if (!metadata.IsEncrypted && !metadata.IsCompressed) {
                var presignedRel = $"{prefix}/files/{fileId:D}/presigned-read";
                if (expiresHours.HasValue)
                    presignedRel += $"?expiresHours={Uri.EscapeDataString(expiresHours.Value.ToString(CultureInfo.InvariantCulture))}";

                try {
                    var presigned = await apiClient.GetAsAsync<PresignedReadResponse>(presignedRel, ct: ct).ConfigureAwait(false);
                    if (presigned?.Url != null
                        && Uri.TryCreate(presigned.Url, UriKind.Absolute, out var presignedUri)
                        && (presignedUri.Scheme == Uri.UriSchemeHttp || presignedUri.Scheme == Uri.UriSchemeHttps))
                        return Results.Redirect(presigned.Url);
                }
                catch {
                    // Presigned not supported or failed — fall through to streaming download from Test API.
                }
            }

            var requestUri = new Uri($"{baseUrl}/{prefix}/files/{fileId:D}/download");
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var client = httpClientFactory.CreateClient(nameof(IApiClient));
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) {
                response.Dispose();
                request.Dispose();
                return Results.StatusCode((int)response.StatusCode);
            }

            var body = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? $"{fileId}";

            var stream = new HttpResponseStream(body, response, request);
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? FileTypeInfo.Unknown.MimeType;
            // Non-seekable proxy stream has no Length — set Content-Length from metadata so browsers show download progress / time remaining.
            if (metadata.OriginalFileSize > 0)
                http.Response.ContentLength = metadata.OriginalFileSize;

            return Results.Stream(stream, mediaType, fileName, enableRangeProcessing: true);
        })
    .WithName("FileStorageWorkbenchProxyDownload");

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();