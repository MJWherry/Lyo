using Lyo.Images.Ocr.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images.Ocr.Tesseract.Tests;

/// <summary>Loads <c>appsettings.json</c> (+ optional Development + env), registers <see cref="IConfiguration"/> in DI. Materializes options via <see cref="ConfigurationBinder"/> (<c>Get&lt;T&gt;()</c>).</summary>
public sealed class TesseractOcrTestFixture : IDisposable
{
    /// <summary>Message passed to xUnit <c>SkipUnless</c> when native OCR integration is turned off.</summary>
    public const string NativeIntegrationDisabledSkipMessage =
        "Native Tesseract integration is disabled. Set OcrTesseractTests:RunIntegration to true in appsettings.Development.json (or local appsettings) or set environment variable LYO_RUN_TESSERACT_INTEGRATION=1.";

    private const string RunIntegrationEnvVar = "LYO_RUN_TESSERACT_INTEGRATION";

    private readonly ServiceProvider _services;

    public TesseractOcrTestFixture()
    {
        Configuration = BuildConfiguration();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(Configuration)
            .BuildServiceProvider();
    }

    public IConfiguration Configuration { get; }

    public IServiceProvider Services => _services;

    public void Dispose() => _services.Dispose();

    /// <summary>OCR engine options bound from <c>OcrEngine</c> via configuration binder.</summary>
    public OcrEngineOptions GetOcrEngineOptions() =>
        Configuration.GetSection(OcrEngineOptions.SectionName).Get<OcrEngineOptions>() ?? new();

    /// <summary>Tesseract subsection bound from <c>OcrEngine:Tesseract</c> via configuration binder.</summary>
    public TesseractOcrEngineOptions GetTesseractOcrEngineOptions() =>
        Configuration.GetSection($"{OcrEngineOptions.SectionName}:{TesseractOcrEngineOptions.ConfigurationKey}").Get<TesseractOcrEngineOptions>() ?? new();

    public bool RunNativeIntegration =>
        (Configuration.GetSection("OcrTesseractTests").Get<OcrTesseractTestsOptions>()?.RunIntegration ?? false)
        || string.Equals(Environment.GetEnvironmentVariable(RunIntegrationEnvVar), "1", StringComparison.Ordinal);

    /// <summary>Returns whether <paramref name="tessdataDirectory"/> contains <c>{languageCode}.traineddata</c> (e.g. <c>jpn</c>, <c>fra</c>).</summary>
    public static bool HasLanguageModel(string? tessdataDirectory, string languageCode)
    {
        if (string.IsNullOrWhiteSpace(tessdataDirectory) || string.IsNullOrWhiteSpace(languageCode))
            return false;

        return File.Exists(Path.Combine(tessdataDirectory.Trim(), $"{languageCode.Trim().ToLowerInvariant()}.traineddata"));
    }

    /// <summary>Directory containing <c>eng.traineddata</c>: explicit test override, then <c>OcrEngine:Tesseract:TessdataDirectory</c>, then discovery.</summary>
    public string ResolveTessdataDirectory()
    {
        var testOpts = Configuration.GetSection("OcrTesseractTests").Get<OcrTesseractTestsOptions>();
        if (TryEngTrainedDataDirectory(testOpts?.TessdataDirectory, out var p))
            return p!;

        var engineTess = GetTesseractOcrEngineOptions();
        if (TryEngTrainedDataDirectory(engineTess.TessdataDirectory, out p))
            return p!;

        var local = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (Directory.Exists(local) && File.Exists(Path.Combine(local, "eng.traineddata")))
            return local;

        var env = Environment.GetEnvironmentVariable("LYO_TESSDATA_DIRECTORY");
        if (TryEngTrainedDataDirectory(env, out p))
            return p!;

        foreach (var candidate in new[] { "/usr/share/tesseract-ocr/5/tessdata", "/usr/share/tesseract-ocr/4.00/tessdata", "/usr/share/tessdata" }) {
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "eng.traineddata")))
                return candidate;
        }

        const string ocrShare = "/usr/share/tesseract-ocr";
        if (Directory.Exists(ocrShare)) {
            foreach (var versionDir in Directory.GetDirectories(ocrShare)) {
                var tessdata = Path.Combine(versionDir, "tessdata");
                if (File.Exists(Path.Combine(tessdata, "eng.traineddata")))
                    return tessdata;
            }
        }

        return "";
    }

    public static string TessdataResolutionHint =>
        "No folder containing eng.traineddata was found. Install tesseract-ocr-eng (or set OcrEngine:Tesseract:TessdataDirectory / OcrTesseractTests:TessdataDirectory in appsettings), "
        + "or download eng.traineddata into tessdata/. Enable integration via OcrTesseractTests:RunIntegration or "
        + RunIntegrationEnvVar
        + "=1.";

    /// <summary>Shown when Japanese OCR tests are skipped because <c>jpn.traineddata</c> is missing from the resolved tessdata folder.</summary>
    public static string JapaneseModelMissingHint =>
        "Install Japanese traineddata in the same tessdata folder as eng (e.g. apt install tesseract-ocr-jpn), "
        + "or download jpn.traineddata from https://github.com/tesseract-ocr/tessdata_fast/raw/main/jpn.traineddata into tessdata/.";

    private static IConfigurationRoot BuildConfiguration()
    {
        var basePath = AppContext.BaseDirectory;
        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    private static bool TryEngTrainedDataDirectory(string? path, out string? fullPath)
    {
        fullPath = null;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var trimmed = path.Trim();
        if (!Directory.Exists(trimmed))
            return false;

        fullPath = Path.GetFullPath(trimmed);
        return File.Exists(Path.Combine(fullPath, "eng.traineddata"));
    }

    /// <summary>Bound from <c>OcrTesseractTests</c> JSON.</summary>
    private sealed class OcrTesseractTestsOptions
    {
        public bool RunIntegration { get; set; }
        public string? TessdataDirectory { get; set; }
    }
}
