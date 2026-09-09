using System.Net;
using Lyo.Exceptions.Models;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpClientUriTests
{
    [Fact]
    public async Task PostAsAsync_RelativeUriWithBaseAddress_Sends()
    {
        var inner = new CaptureHandler();
        using var client = new LyoHttpClient(new HttpClient(inner) { BaseAddress = new("http://localhost:5251/") });
        await client.PostAsAsync<object, StubDto>("Person/QueryProject", new(), ct: TestContext.Current.CancellationToken);
        Assert.Equal(new Uri("http://localhost:5251/Person/QueryProject"), inner.RequestUri);
    }

    [Fact]
    public async Task PostAsAsync_RelativeUriWithoutBaseAddress_Throws()
    {
        using var client = new LyoHttpClient(new HttpClient());
        var ex = await Assert.ThrowsAsync<InvalidFormatException>(
            () => client.PostAsAsync<object, StubDto>("Person/QueryProject", new(), ct: TestContext.Current.CancellationToken));
        Assert.Contains("Person/QueryProject", ex.Message, StringComparison.Ordinal);
    }

    private sealed record StubDto(int N);

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"n\":1}") });
        }
    }
}
