using System.Net;
using System.Text;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models;
using Lyo.Api.Models.Error;
using Lyo.Common.Json;
using Lyo.Exceptions.Models;

namespace Lyo.Api.Tests;

public sealed class ApiClientProblemDetailsUnitTests
{
    [Fact]
    public async Task GetAsAsync_NonSuccessJson_ThrowsApiExceptionWithProblemDetails()
    {
        var problem = LyoProblemDetails.FromCode(Constants.ApiErrorCodes.NotFound, "Widget missing");
        var json = JsonSerializer.Serialize(problem, LyoJsonSerializerOptions.Create());
        var inner = new StubHandler(new HttpResponseMessage(HttpStatusCode.NotFound) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        using var client = new ApiClient(httpClient: new HttpClient(inner) { BaseAddress = new("https://example.test/") });
        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsAsync<object>("/missing", ct: TestContext.Current.CancellationToken));
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal(Constants.ApiErrorCodes.NotFound, ex.ErrorCode);
        Assert.NotNull(ex.ProblemDetails);
        Assert.False(ex.IsTransient);
        Assert.IsAssignableFrom<HttpException>(ex);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(response);
    }
}
