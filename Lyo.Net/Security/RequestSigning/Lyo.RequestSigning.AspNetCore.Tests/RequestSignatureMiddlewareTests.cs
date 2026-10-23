using System.Text;
using Lyo.Hashing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.RequestSigning.AspNetCore.Tests;

public sealed class RequestSignatureMiddlewareTests
{
    [Fact]
    public async Task Middleware_ValidSignature_CallsNext()
    {
        var runs = 0;
        await using var host = await HostAsync(app => app.MapPost(
            "/pay", () => {
                runs++;
                return Results.Ok("paid");
            }));

        var body = Encoding.UTF8.GetBytes("{\"a\":1}");
        var header = Header(host, body);
        var status = await SendAsync(host, body, header);
        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task Middleware_BadSignature_Returns401()
    {
        var runs = 0;
        await using var host = await HostAsync(app => app.MapPost(
            "/pay", () => {
                runs++;
                return Results.Ok("paid");
            }));

        var status = await SendAsync(host, Encoding.UTF8.GetBytes("{\"a\":1}"), "t=1,v1=" + new string('a', 64));
        Assert.Equal(StatusCodes.Status401Unauthorized, status);
        Assert.Equal(0, runs);
    }

    private static async Task<WebApplication> HostAsync(Action<WebApplication> map)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IHashingService>(HashingService.Shared);
        builder.Services.AddRequestSigning(new RequestSignatureOptions { Key = "current-signing-key" });
        var app = builder.Build();
        app.UseRequestSignature();
        map(app);
        await app.StartAsync(TestCt);
        return app;
    }

    private static string Header(WebApplication host, byte[] body)
    {
        var signer = host.Services.GetRequiredService<RequestSigner>();
        var input = new RequestSignatureInput("POST", "http", "localhost", "/pay", body);
        return signer.CreateHeader(input);
    }

    private static async Task<int> SendAsync(WebApplication host, byte[] body, string header)
    {
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/pay") { Content = new ByteArrayContent(body) };
        request.Headers.TryAddWithoutValidation("Lyo-Signature", header);
        var response = await client.SendAsync(request, TestCt);
        return (int)response.StatusCode;
    }
}