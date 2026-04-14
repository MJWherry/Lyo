using System.Globalization;
using Lyo.Csv.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Csv.Tests;

public class CsvExtensionsTests
{
    [Fact]
    public void AddCsvService_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCsvService();
        var provider = services.BuildServiceProvider();
        var csvService = provider.GetRequiredService<ICsvService>();
        Assert.NotNull(csvService);
    }

    [Fact]
    public void AddCsvService_WithConfigure_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCsvService(cfg => cfg.Delimiter = ";");
        var provider = services.BuildServiceProvider();
        var csvService = provider.GetRequiredService<ICsvService>();
        Assert.NotNull(csvService);
    }

    [Fact]
    public void AddCsvService_WithConfigBuilder_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCsvService(() => new(CultureInfo.InvariantCulture) { Delimiter = "|" });
        var provider = services.BuildServiceProvider();
        var csvService = provider.GetRequiredService<ICsvService>();
        Assert.NotNull(csvService);
    }

    [Fact]
    public void AddCsvService_WithServiceProviderConfig_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCsvService((sp, cfg) => cfg.Delimiter = ",");
        var provider = services.BuildServiceProvider();
        var csvService = provider.GetRequiredService<ICsvService>();
        Assert.NotNull(csvService);
    }
}