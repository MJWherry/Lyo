using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Lyo.Api.Tests;

/// <summary>WebApplicationFactory that injects PostgreSQL connection string for integration tests.</summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ApiWebApplicationFactory(string connectionString) => _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureAppConfiguration((context, config) => {
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["PostgresJob:ConnectionString"] = _connectionString, ["PostgresJob:EnableAutoMigrations"] = "false" });
        });

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config => {
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["PostgresJob:ConnectionString"] = _connectionString, ["PostgresJob:EnableAutoMigrations"] = "false" });
        });

        return base.CreateHost(builder);
    }
}