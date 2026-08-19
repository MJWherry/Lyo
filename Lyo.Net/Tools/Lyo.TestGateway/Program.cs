using Lyo.Api.Models.Error;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using System.Globalization;
using Blazored.LocalStorage;
using Lyo.Api.Client;
using Lyo.Authentication.Client;
using Lyo.Authentication.Web.Components;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Options;
using Lyo.Authentication.Web.Components.Server;
using Lyo.Barcode.Native;
using Lyo.Cache;
using Lyo.Common;
using Lyo.Common.Records;
using Lyo.Compression;
using Lyo.Csv;
using Lyo.DateAndTime.Json;
using Lyo.Email;
using Lyo.Endato.Client;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Web.Components.Services;
using Lyo.Formatter;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Services;
using Lyo.TestGateway.Stores;
using Lyo.Images;
using Lyo.IO.Temp;
using Lyo.Lock;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Metrics;
using Lyo.Pdf;
using Lyo.Pdf.Web.Components.PdfAnnotator;
using Lyo.Profanity;
using Lyo.QRCode;
using Lyo.Scheduler;
using Lyo.Sms.Twilio;
using Lyo.Translation.Aws;
using Lyo.Tts.Typecast;
using Lyo.Typecast.Client;
using Lyo.Web.Components;
using Lyo.Web.Components.Export;
using Lyo.Web.WebRenderer;
using Lyo.Xlsx;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using Constants = Lyo.TestGateway.Models.Constants;

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
builder.Services.AddLyoMetrics();
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
builder.Services.AddFileStorageWorkbenchSupport(builder.Configuration);
builder.Services.AddFormatterService();
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

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddPdfService();
builder.Services.AddScoped<IJsInterop, JsInterop>();
builder.Services.TryAddScoped<ILyoTimeZone, LyoBrowserTimeZone>();
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
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapLyoAuthSignIn();
app.MapLyoAuthHandoffCallback();
app.MapLyoAuthSignOut();

// File Storage Workbench download: asks Test API for a time-limited storage URL when safe (plain files → e.g. S3 presigned), redirects the browser there so bytes never cross Gateway; otherwise streams decrypted output from Test API.
app.MapGet(
        $"/{Constants.FileStorageWorkbench.ProxyDownloadRoute}/{{fileId:guid}}", async (
            HttpContext http, Guid fileId, string? fileName, double? expiresHours, IApiClient apiClient, IHttpClientFactory httpClientFactory,
            IOptions<FileStorageWorkbenchOptions> fsw, IOptions<ApiClientOptions> apiOptions, CancellationToken ct) => {
            if (!fsw.Value.UseRemoteApiServices) {
                var workbenchMisconfigured = LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, "File storage workbench is not configured to use Test API services.");
                return Results.Json(workbenchMisconfigured, statusCode: workbenchMisconfigured.Status, contentType: "application/problem+json");
            }

            var baseUrl = apiOptions.Value.BaseUrl?.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl)) {
                var missingBaseUrl = LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, "ApiClient:BaseUrl is not configured; cannot download via workbench.");
                return Results.Json(missingBaseUrl, statusCode: missingBaseUrl.Status, contentType: "application/problem+json");
            }

            var prefix = fsw.Value.ApiRoutePrefix.Trim().Trim('/');
            FileStoreResult? metadata;
            try {
                metadata = await apiClient.GetAsAsync<FileStoreResult>($"{prefix}/files/{fileId:D}/metadata?includeDeleted=false", ct: ct).ConfigureAwait(false);
            }
            catch {
                var notFound = LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found.");
                return Results.Json(notFound, statusCode: notFound.Status, contentType: "application/problem+json");
            }

            if (metadata == null) {
                var notFound = LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found.");
                return Results.Json(notFound, statusCode: notFound.Status, contentType: "application/problem+json");
            }

            var downloadName = FirstNonEmpty(fileName, metadata.OriginalFileName, metadata.SourceFileName) ?? $"{fileId:D}";
            var disposition = AttachmentContentDisposition(downloadName);
            // Plain objects: use IFileStorageService time-limited read URL (S3/Azure presigned, etc.) so the browser loads directly from storage.
            if (!metadata.IsEncrypted && !metadata.IsCompressed) {
                var qs = new List<string> { $"contentDisposition={Uri.EscapeDataString(disposition)}" };
                if (expiresHours.HasValue)
                    qs.Add($"expiresHours={Uri.EscapeDataString(expiresHours.Value.ToString(CultureInfo.InvariantCulture))}");
                if (!string.IsNullOrWhiteSpace(metadata.ContentType))
                    qs.Add($"contentType={Uri.EscapeDataString(metadata.ContentType)}");

                var presignedRel = $"{prefix}/files/{fileId:D}/presigned-read?{string.Join("&", qs)}";
                try {
                    var presigned = await apiClient.GetAsAsync<PresignedReadResponse>(presignedRel, ct: ct).ConfigureAwait(false);
                    if (presigned?.Url != null && Uri.TryCreate(presigned.Url, UriKind.Absolute, out var presignedUri) &&
                        (presignedUri.Scheme == Uri.UriSchemeHttp || presignedUri.Scheme == Uri.UriSchemeHttps))
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
            var resolvedFileName = response.Content.Headers.ContentDisposition?.FileNameStar
                                   ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                                   ?? downloadName;
            var stream = new HttpResponseStream(body, response, request);
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? metadata.ContentType ?? FileTypeInfo.Unknown.MimeType;
            // Non-seekable proxy stream has no Length — set Content-Length from metadata so browsers show download progress / time remaining.
            if (metadata.OriginalFileSize > 0)
                http.Response.ContentLength = metadata.OriginalFileSize;

            return Results.Stream(stream, mediaType, resolvedFileName, enableRangeProcessing: true);
        })
    .WithName("FileStorageWorkbenchProxyDownload");

static string? FirstNonEmpty(params string?[] values)
{
    foreach (var value in values) {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();
    }

    return null;
}

static string AttachmentContentDisposition(string fileName)
{
    var leaf = Path.GetFileName(fileName.Replace('\\', '/'));
    if (string.IsNullOrWhiteSpace(leaf))
        leaf = "download";

    var ascii = leaf.Replace("\"", "'", StringComparison.Ordinal);
    return $"attachment; filename=\"{ascii}\"; filename*=UTF-8''{Uri.EscapeDataString(leaf)}";
}

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();