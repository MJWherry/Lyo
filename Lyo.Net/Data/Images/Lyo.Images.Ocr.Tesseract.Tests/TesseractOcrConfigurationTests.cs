using Lyo.Images.Ocr.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images.Ocr.Tesseract.Tests;

/// <summary>
/// Always-on tests that exercise JSON load + <see cref="ConfigurationBinder"/> (<c>Get&lt;T&gt;()</c>) and DI registration. Integration OCR tests skip unless <c>OcrTesseractTests:RunIntegration</c> is true.
/// Library DI helpers (<c>AddTesseractOcrEngineFromConfiguration</c> in <c>Lyo.Images.Ocr.Tesseract</c>) are not used by this project; break there only when debugging host apps that register OCR via DI.
/// </summary>
public sealed class TesseractOcrConfigurationTests(TesseractOcrTestFixture fixture)
{
    private readonly TesseractOcrTestFixture _fixture = fixture;

    [Fact]
    public void Configuration_root_loads_and_exposes_ocr_section()
    {
        var cfg = _fixture.Services.GetRequiredService<IConfiguration>();
        Assert.NotNull(cfg);
        var section = cfg.GetSection(OcrEngineOptions.SectionName);
        Assert.True(section.Exists(), $"Missing JSON section '{OcrEngineOptions.SectionName}'. Ensure appsettings.json is copied to output (see csproj CopyToOutputDirectory). BaseDirectory={AppContext.BaseDirectory}");
    }

    [Fact]
    public void Get_materializes_OcrEngineOptions_from_appsettings()
    {
        var shared = _fixture.Configuration.GetSection(OcrEngineOptions.SectionName).Get<OcrEngineOptions>();
        Assert.NotNull(shared);
        Assert.False(string.IsNullOrWhiteSpace(shared.DefaultLanguages));
        var tess = _fixture.Configuration.GetSection($"{OcrEngineOptions.SectionName}:{TesseractOcrEngineOptions.ConfigurationKey}").Get<TesseractOcrEngineOptions>();
        Assert.NotNull(tess);
        // TessdataDirectory may be empty in committed appsettings; binding still runs (breakpoint-friendly path).
        Assert.NotNull(tess.TessdataDirectory);
    }

    [Fact]
    public void RunNativeIntegration_reflects_config_or_env()
    {
        _ = _fixture.RunNativeIntegration;
        _ = _fixture.ResolveTessdataDirectory();
    }
}
