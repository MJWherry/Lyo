using Blazored.LocalStorage;
using Lyo.Api.Client;
using Lyo.Cache;
using Lyo.Common;
using Lyo.Compression;
using Lyo.Csv;
using Lyo.DateAndTime.Json;
using Lyo.Lock;
using Lyo.Metrics;
using Lyo.Portfolio.Components;
using Lyo.Portfolio.Services;
using Lyo.Scheduler;
using Lyo.Web.Components;
using Lyo.Xlsx;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Lyo services
builder.Services.AddCsvService();
builder.Services.AddXlsxService();
builder.Services.AddCompressionService();
builder.Services.AddLyoMetrics();
builder.Services.AddScheduler();
builder.Services.AddLocalCacheFromConfiguration(builder.Configuration);
builder.Services.AddLocalLock(options => options.EnableMetrics = true);
builder.Services.AddLocalKeyedSemaphore(options => options.EnableMetrics = true);
builder.Services.AddScoped<PortfolioFileTransformer>();
builder.Services.Configure<ApiClientOptions>(builder.Configuration.GetSection(ApiClientOptions.SectionName));
builder.Services.AddTransient(provider => provider.GetRequiredService<IOptions<ApiClientOptions>>().Value);
builder.Services.AddLyoApiClient();
builder.Services.AddSingleton(_ => {
    var options = LyoJsonSerializerOptions.Create();
    options.AddLyoDateOnlyModelConverters();
    options.WriteIndented = true;
    return options;
});

// Web components
builder.Services.AddScoped<IJsInterop, JsInterop>();
builder.Services.AddSingleton(_ => LyoJsonSerializerOptions.Create(o => o.WriteIndented = true));
builder.Services.AddHttpClient();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ClientStore>();
builder.Services.AddMudServices(config => {
    config.PopoverOptions.ModalOverlay = true;
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

await builder.Build().RunAsync();