using System.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions.Models;
using Lyo.Http.Client;
using Lyo.Http.Client.Pipeline;
using Lyo.Http.Client.Plan;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpClientHookTests
{
    [Fact]
    public async Task Hooks_FireInOrder()
    {
        var order = new List<string>();
        var inner = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"n\":1}") });
        var hooks = new LyoHttpClientHooks {
            BeforeSendAsync = (_, _) => { order.Add("before"); return Task.CompletedTask; },
            OnRequestObserved = (_, _) => { order.Add("req"); return Task.CompletedTask; },
            AfterResponseAsync = (_, _) => { order.Add("after"); return Task.CompletedTask; },
            OnResponseObserved = (_, _) => { order.Add("res"); return Task.CompletedTask; }
        };
        using var client = new LyoHttpClient(new HttpClient(inner) { BaseAddress = new("https://example.test/") }, hooks: hooks);
        await client.GetAsAsync<StubDto>("/x", ct: TestContext.Current.CancellationToken);
        Assert.Equal(["before", "req", "after", "res"], order);
    }

    private sealed record StubDto(int N);

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(response);
    }
}

