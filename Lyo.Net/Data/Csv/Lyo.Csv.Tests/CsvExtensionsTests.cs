using Lyo.Csv.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        var options = provider.GetRequiredService<IOptions<CsvOptions>>().Value;
        Assert.False(options.Pooling.PoolValues);
    }

    [Fact]
    public void AddCsvService_WithCsvOptions_EnablesPooling()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCsvService(o => {
            o.Pooling.PoolValues = true;
            o.Pooling.PoolingCellThreshold = 0;
        });
        var provider = services.BuildServiceProvider();
        Assert.True(provider.GetRequiredService<IOptions<CsvOptions>>().Value.Pooling.PoolValues);
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
    public void AddCsvService_WithOptionsInstance_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCsvService(new CsvOptions { Delimiter = "|" });
        var provider = services.BuildServiceProvider();
        var csvService = provider.GetRequiredService<ICsvService>();
        Assert.NotNull(csvService);
        Assert.Equal("|", provider.GetRequiredService<IOptions<CsvOptions>>().Value.Delimiter);
    }

    [Fact]
    public void AddCsvService_WithServiceProviderConfig_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCsvService((_, cfg) => cfg.Delimiter = ",");
        var provider = services.BuildServiceProvider();
        var csvService = provider.GetRequiredService<ICsvService>();
        Assert.NotNull(csvService);
    }
}
