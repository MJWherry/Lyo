using Lyo.Api.ApiEndpoint;
using Lyo.Reporting.Api;
using Lyo.Reporting.Postgres;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Tests;

public sealed class ReportingApiOptionsTests
{
    [Fact]
    public void Constructor_DefaultAuth_RequiresAuthorization()
    {
        var options = new ReportingApiOptions();
        foreach (var auth in new[] { options.DefinitionAuth, options.GenerationAuth, options.GenerateAuth, options.DownloadAuth }) {
            Assert.NotNull(auth);
            Assert.False(auth!.AllowAnonymous);
        }
    }

    [Fact]
    public void Constructor_DownloadStreamFactory_DefaultsToNull() => Assert.Null(new ReportingApiOptions().DownloadStreamFactory);

    [Fact]
    public void WithAuth_AllSurfaces_ShareAuth()
    {
        var auth = EndpointAuth.Anonymous();
        Func<ReportDownloadContext, CancellationToken, Task<Stream?>> factory = (_, _) => Task.FromResult<Stream?>(null);
        var options = ReportingApiOptions.WithAuth(auth, factory);
        Assert.Same(auth, options.DefinitionAuth);
        Assert.Same(auth, options.GenerationAuth);
        Assert.Same(auth, options.GenerateAuth);
        Assert.Same(auth, options.DownloadAuth);
        Assert.Same(factory, options.DownloadStreamFactory);
    }

    [Fact]
    public void AddReportingApi_ManagementAndApiServices_Registers()
    {
        var services = new ServiceCollection();
        services.AddReportingApi(o => o.ConnectionString = "Host=localhost;Database=reporting_test");
        Assert.Contains(services, d => d.ServiceType == typeof(ReportService));
        Assert.Contains(services, d => d.ServiceType == typeof(ReportRetentionService));
        Assert.Contains(services, d => d.ServiceType == typeof(ReportGenerationThrottle));
        Assert.Contains(services, d => d.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void AddReportingApi_InvalidOptions_Throws() => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddReportingApi(new PostgresReportingOptions()));
}
