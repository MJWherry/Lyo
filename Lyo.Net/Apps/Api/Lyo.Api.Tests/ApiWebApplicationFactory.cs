using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Lyo.Api.Tests;

/// <summary>WebApplicationFactory that injects PostgreSQL connection string for integration checks.</summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings;

    /// <param name="connectionString">Postgres connection string for the container backing the test.</param>
    /// <param name="overrides">Extra configuration entries, for example a different <c>CacheOptions:QueryCacheTagGranularity</c> for tests that need per-row tags.</param>
    public ApiWebApplicationFactory(string connectionString, IReadOnlyDictionary<string, string?>? overrides = null)
    {
        _settings = new() { ["PostgresJob:ConnectionString"] = connectionString, ["PostgresJob:EnableAutoMigrations"] = "false" };
        if (overrides is null)
            return;

        foreach (var (key, value) in overrides)
            _settings[key] = value;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(_settings));

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(_settings));
        return base.CreateHost(builder);
    }
}