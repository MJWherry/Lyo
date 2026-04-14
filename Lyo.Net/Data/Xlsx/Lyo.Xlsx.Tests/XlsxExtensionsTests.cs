using Lyo.Xlsx.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Xlsx.Tests;

public class XlsxExtensionsTests
{
    [Fact]
    public void AddXlsxService_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddXlsxService();
        var provider = services.BuildServiceProvider();
        var xlsxService = provider.GetRequiredService<IXlsxService>();
        Assert.NotNull(xlsxService);
        var exporter = provider.GetRequiredService<IXlsxExporter>();
        Assert.NotNull(exporter);
        var importer = provider.GetRequiredService<IXlsxImporter>();
        Assert.NotNull(importer);
    }
}