using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Billing;
using Lyo.Billing.Client;
using Lyo.Common.Metadata.Records;
using Lyo.Http.Client;
using Lyo.Http.Client.Plan;
using Lyo.Http.Client.Session;
using Lyo.Query.Models.Common.Request;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Billing.Client.Tests;

[Trait("Category", "Fast")]
public sealed class BillingClientTests
{
    [Fact]
    public void InvoicePdfHref_WhenBaseAddressSet_CombinesHostAndPath()
    {
        var api = new RecordingApiClient(new Uri("http://localhost:5251/"));
        var client = new BillingClient(api);
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        Assert.Equal("http://localhost:5251/Reporting/Invoices/aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee/pdf", client.InvoicePdfHref(id));
    }

    [Fact]
    public void InvoicePdfHref_WhenRoutePrefix_IncludesPrefix()
    {
        var api = new RecordingApiClient(new Uri("http://localhost:5251/"));
        var client = new BillingClient(api, new BillingClientOptions { RoutePrefix = "api" });
        var id = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        Assert.Equal("http://localhost:5251/api/Reporting/Invoices/aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee/pdf", client.InvoicePdfHref(id));
    }

    [Fact]
    public void Validate_SchemeInPrefix_Throws()
    {
        var options = new BillingClientOptions { RoutePrefix = "http://localhost" };
        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public async Task CreateDraftAsync_PostsDraftRoute()
    {
        var api = new RecordingApiClient();
        var client = new BillingClient(api);
        var req = new CreateDraftInvoiceRequest { ClientId = LyoGuid.CreateV7(), TaxRate = 0.08m };
        await client.CreateDraftAsync(req, TestCt);
        Assert.Equal("Billing/Invoices/draft", api.LastUri);
        Assert.Same(req, api.LastBody);
    }

    [Fact]
    public async Task IssueAsync_WhenTenant_AppendsQuery()
    {
        var api = new RecordingApiClient();
        var client = new BillingClient(api);
        var invoiceId = LyoGuid.CreateV7();
        var tenantId = Guid.Parse("00000000-0000-4000-8000-000000000001");
        await client.IssueAsync(invoiceId, tenantId, TestCt);
        Assert.Equal($"Billing/Invoices/{invoiceId:D}/issue?tenantId={tenantId:D}", api.LastUri);
    }
}

internal sealed class RecordingApiClient : IApiClient
{
    private readonly HttpClient _http = new();

    public RecordingApiClient(Uri? baseAddress = null)
    {
        if (baseAddress != null)
            _http.BaseAddress = baseAddress;
    }

    public string? LastUri { get; private set; }

    public object? LastBody { get; private set; }

    public void Dispose() => _http.Dispose();

    public JsonSerializerOptions GetSerializerOptions() => new();

    public HttpClient GetClient() => _http;

    public ILyoHttpSession Session { get; } = new LyoHttpSession();

    public LyoHttpClientHooks Hooks { get; } = new();

    public HttpClientPlanBuilder Get(string uri) => throw new NotImplementedException(uri);

    public HttpClientPlanBuilder Post(string uri) => throw new NotImplementedException(uri);

    public Task<HttpClientPlanRunResult> RunAsync(HttpClientPlan plan, HttpClientPlanRuntime? runtime = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        LastBody = request;
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult));
    }

    public Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query = default,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException(uri);

    public Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<string> DownloadToFileAsync(
        string uri,
        string destinationPath,
        IProgress<LyoHttpDownloadProgress>? progress = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult)!);
    }

    public string ToQueryString<T>(T obj, string? enumerableDelimiter = null) => throw new NotImplementedException();

    public Task<TResult?> QueryProjectAsync<TResult>(string route, ProjectionQueryReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(route);

    public Task<TResult?> QueryConcreteAsync<TResult>(string route, QueryConcreteReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(route);

    public Task<TResult?> QuerySqlAsync<TResult>(string route, QueryRequestBase request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(route);
}
