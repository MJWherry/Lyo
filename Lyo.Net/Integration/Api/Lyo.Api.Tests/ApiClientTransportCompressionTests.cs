using System.Text;
using Lyo.Api;
using Lyo.Api.Client;
using Lyo.Common.Records;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Tests;

public sealed class ApiClientTransportCompressionTests
{
    private static readonly string Payload = string.Join('\n', Enumerable.Repeat("name,value,extra", 80));

    [Fact]
    public async Task GetFileAsync_AndPostAsBinaryAsync_ReturnDecodedCsv()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddLyoApiCompression();
        await using var app = builder.Build();
        app.UseLyoApiCompression();
        app.MapGet("/file", () => Results.Text(Payload, FileTypeInfo.Csv.MimeType));
        app.MapPost("/file", () => Results.Text(Payload, FileTypeInfo.Csv.MimeType));
        await app.StartAsync(TestContext.Current.CancellationToken);
        var baseUrl = app.Urls.Single();

        using var client = new ApiClient(options: new() { BaseUrl = baseUrl });
        var getBytes = await client.GetFileAsync("file", ct: TestContext.Current.CancellationToken);
        Assert.Equal(Payload, Encoding.UTF8.GetString(getBytes));

        var postBytes = await client.PostAsBinaryAsync<object?>("file", null, ct: TestContext.Current.CancellationToken);
        Assert.Equal(Payload, Encoding.UTF8.GetString(postBytes));
    }
}
