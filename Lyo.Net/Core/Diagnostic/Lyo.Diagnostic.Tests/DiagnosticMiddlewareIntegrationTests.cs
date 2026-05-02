using Lyo.Diagnostic.AspNetCore;
using Lyo.Diagnostic.Inbox;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lyo.Diagnostic.Tests;

public sealed class DiagnosticMiddlewareIntegrationTests
{
    [Fact]
    public async Task UseDiagnosticExceptionRecording_RecordsInbox_OnUnhandledException()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => {
                web.UseTestServer();
                web.ConfigureServices(services => {
                    services.AddLogging();
                    services.AddLyoDiagnosticsWeb();
                });
                web.Configure(app => {
                    app.UseDiagnosticExceptionRecording();
                    app.Use(async (HttpContext _, RequestDelegate _) =>
                    {
                        throw new InvalidOperationException("boom");
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        var client = host.GetTestServer().CreateClient();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/", TestContext.Current.CancellationToken));
        Assert.Equal("boom", ex.Message);

        var inbox = host.Services.GetRequiredService<InMemoryErrorInbox>();
        var groups = inbox.ListGroups(TimeSpan.FromMinutes(1));
        Assert.NotEmpty(groups);
        Assert.True(groups[0].OccurrenceCount >= 1);
    }
}
