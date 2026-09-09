using Lyo.Pdf.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Pdf.Tests;

public class PdfExtensionsTests
{
    [Fact]
    public void AddPdfService_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPdfService();
        var provider = services.BuildServiceProvider();
        var pdfService = provider.GetRequiredService<IPdfService>();
        Assert.NotNull(pdfService);
    }

    [Fact]
    public void AddPdfService_WithConfigure_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPdfService(opts => opts.DefaultYTolerance = 8.0);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PdfServiceOptions>();
        Assert.Equal(8.0, options.DefaultYTolerance);
        var pdfService = provider.GetRequiredService<IPdfService>();
        Assert.NotNull(pdfService);
    }

    [Fact]
    public void AddPdfService_WithConfiguration_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configData = new Dictionary<string, string?> { ["PdfServiceOptions:DefaultYTolerance"] = "10" };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        services.AddPdfServiceFromConfiguration(config);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PdfServiceOptions>();
        Assert.Equal(10.0, options.DefaultYTolerance);
    }

    [Fact]
    public void AddPdfServiceKeyed_RegistersKeyedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPdfServiceKeyed("pdf1");
        var provider = services.BuildServiceProvider();
        var pdfService = provider.GetRequiredKeyedService<IPdfService>("pdf1");
        Assert.NotNull(pdfService);
    }
}