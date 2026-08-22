using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Tests.Fixtures;
using Lyo.Common;
using Lyo.Common.Records;
using Lyo.Job.Models.Response;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Tests;

[Collection(ApiPostgresCollection.Name)]
public class CompressionPostgresTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = LyoJsonSerializerOptions.Create();

    private readonly HttpClient _client;
    private readonly ApiPostgresFixture _fixture;

    public CompressionPostgresTests(ApiPostgresFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Get_WithBrotliAcceptEncoding_ReturnsBrotliCompressedResponse()
    {
        var id = await _fixture.SeedJobDefinitionAsync("CompressionGet");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Job/Definition/{id}");
        request.Headers.AcceptEncoding.Add(new("br"));
        using var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        Assert.Contains("br", response.Content.Headers.ContentEncoding, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Accept-Encoding", response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        var compressedBytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        var jsonBytes = DecompressBrotli(compressedBytes);
        using var doc = JsonDocument.Parse(jsonBytes);
        var json = doc.RootElement.GetRawText();
        Assert.Contains("CompressionGet", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_WithGzipContentEncoding_ProcessesCompressedRequestBody()
    {
        await _fixture.SeedJobDefinitionAsync("CompressionQueryTarget");
        var requestBody = new QueryConcreteReq {
            Start = 0, Amount = 10, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "CompressionQueryTarget")
        };

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(requestBody, JsonOptions);
        var gzippedBytes = CompressGzip(jsonBytes);
        using var content = new ByteArrayContent(gzippedBytes);
        content.Headers.ContentType = new(FileTypeInfo.Json.MimeType);
        content.Headers.ContentEncoding.Add("gzip");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Job/Definition/QueryConcrete");
        request.Content = content;
        using var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<QueryRes<JobDefinitionRes>>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("CompressionQueryTarget", result.Items[0].Name);
    }

    [Fact]
    public async Task ExportCsv_WithBrotliAcceptEncoding_ReturnsCompressedCsv()
    {
        var name = $"CompressionCsv_{Guid.NewGuid():N}";
        await _fixture.SeedJobDefinitionAsync(name);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Job/JobDefinition/Export") {
            Content = JsonContent.Create(
                new ExportRequest {
                    Query = new() { Start = 0, Amount = 10, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, name) }, Format = ExportFormat.Csv
                }, options: JsonOptions)
        };
        request.Headers.AcceptEncoding.Add(new("br"));
        using var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        Assert.Contains("br", response.Content.Headers.ContentEncoding, StringComparer.OrdinalIgnoreCase);
        var csv = Encoding.UTF8.GetString(DecompressBrotli(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)));
        Assert.Contains(name, csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportXlsx_WithBrotliAcceptEncoding_IsNotCompressed()
    {
        var name = $"CompressionXlsx_{Guid.NewGuid():N}";
        await _fixture.SeedJobDefinitionAsync(name);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Job/JobDefinition/Export") {
            Content = JsonContent.Create(
                new ExportRequest {
                    Query = new() { Start = 0, Amount = 10, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, name) }, Format = ExportFormat.Xlsx
                }, options: JsonOptions)
        };
        request.Headers.AcceptEncoding.Add(new("br"));
        using var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain("br", response.Content.Headers.ContentEncoding, StringComparer.OrdinalIgnoreCase);
    }

    private static byte[] CompressGzip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, true))
            gzip.Write(data, 0, data.Length);

        return output.ToArray();
    }

    private static byte[] DecompressBrotli(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }
}