using System.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions.Models;
using Lyo.Http.Client;
using Lyo.Http.Client.Pipeline;
using Lyo.Http.Client.Plan;

namespace Lyo.Http.Client.Tests;

public sealed class HttpClientPlanFailureTests
{
    [Fact]
    public async Task RunAsync_TransportException_IncludesInnerMessageAndException()
    {
        var inner = new InvalidOperationException("remote cert untrusted");
        var handler = new ThrowHandler(new HttpRequestException("The SSL connection could not be established, see inner exception.", inner));
        using var client = new LyoHttpClient(new HttpClient(handler) { BaseAddress = new("https://example.test/") });
        var result = await client.RunAsync(HttpClientPlanBuilder.New().Get("/x").Commit().Build(), ct: TestContext.Current.CancellationToken);
        Assert.False(result.Success);
        Assert.Null(result.LastStatusCode);
        Assert.Contains("SSL connection could not be established", result.Error, StringComparison.Ordinal);
        Assert.Contains("remote cert untrusted", result.Error, StringComparison.Ordinal);
        Assert.Same(inner, result.Exception?.InnerException);
    }

    private sealed class ThrowHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromException<HttpResponseMessage>(exception);
    }
}
