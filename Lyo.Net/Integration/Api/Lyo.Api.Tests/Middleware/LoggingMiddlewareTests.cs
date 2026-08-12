using System.Net;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Middleware;
using Lyo.Api.Models;
using Lyo.Api.Models.Error;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Lyo.Api.Tests.Middleware;

/// <summary>End-to-end tests for <see cref="LoggingMiddleware" /> exception-to-status mapping and the empty-body problem details fallback, using an in-memory test server.</summary>
public class LoggingMiddlewareTests
{
    private static readonly JsonSerializerOptions SerializerOptions = LyoJsonSerializerOptions.Create();

    private static async Task<IHost> StartHostAsync()
        => await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices(services => {
                    services.AddLogging();
                    services.AddRouting();
                })
                .Configure(app => {
                    app.UseMiddleware<LoggingMiddleware>();
                    app.UseRouting();
                    app.UseEndpoints(endpoints => {
                        endpoints.MapGet("/throw/not-found", IResult () => throw new NotFoundException("Widget", 42));
                        endpoints.MapGet("/throw/error-code", IResult () => throw new BadRequestException("Duplicate widget name.") { ErrorCode = "widget.duplicate_name" });
                        endpoints.MapGet("/throw/validation", IResult () => throw new ValidationException("Name", "Name is required."));
                        endpoints.MapGet("/throw/rate-limit", IResult () => throw new RateLimitExceededException(TimeSpan.FromSeconds(30)));
                        endpoints.MapGet("/throw/unhandled", IResult () => throw new InvalidOperationException("boom"));
                        endpoints.MapGet(
                            "/throw/api-error", IResult () => throw ApiErrorException.From(
                                LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidSelectField, "Select field 'Foo.Bar' is not valid for type 'Person'.")));
                        endpoints.MapGet(
                            "/throw/api-error-forbidden", IResult () => throw ApiErrorException.From(
                                LyoProblemDetails.FromCode(Constants.ApiErrorCodes.Forbidden, "Caller cannot access this resource.")));
                        endpoints.MapGet("/bare/not-found", () => Results.NotFound());
                        endpoints.MapGet("/bare/unauthorized", () => Results.Unauthorized());
                        endpoints.MapGet(
                            "/challenge/unauthorized", async (HttpContext ctx) => {
                                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                ctx.Response.Headers.WWWAuthenticate = "Bearer";
                                await ctx.Response.StartAsync();
                            });
                        endpoints.MapGet("/ok", () => Results.Ok(new { value = 1 }));
                    });
                }))
            .StartAsync(TestContext.Current.CancellationToken);

    private static async Task<LyoProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var problem = JsonSerializer.Deserialize<LyoProblemDetails>(json, SerializerOptions);
        Assert.NotNull(problem);
        return problem;
    }

    [Fact]
    public async Task ThrownNotFoundException_Returns404ProblemDetails()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/throw/not-found", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(404, problem.Status);
        Assert.Equal(Constants.ApiErrorCodes.NotFound, problem.Errors[0].Code);
        Assert.Contains("Widget", problem.Detail);
        Assert.Equal("/throw/not-found", problem.Instance);
    }

    [Fact]
    public async Task ThrownHttpException_PreservesCustomErrorCode()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/throw/error-code", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("widget.duplicate_name", problem.Errors[0].Code);
    }

    [Fact]
    public async Task ThrownValidationException_Returns400WithFieldErrors()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/throw/validation", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(Constants.ApiErrorCodes.ValidationFailed, problem.Errors[0].Code);
        Assert.Contains("Name", problem.Errors[0].Description);
    }

    [Fact]
    public async Task ThrownRateLimitException_Returns429WithRetryAfterHeader()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/throw/rate-limit", TestContext.Current.CancellationToken);
        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), response.Headers.RetryAfter?.Delta);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(Constants.ApiErrorCodes.TooManyRequests, problem.Errors[0].Code);
    }

    [Fact]
    public async Task UnhandledException_StillReturns500ProblemDetails()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/throw/unhandled", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(500, problem.Status);
        Assert.Equal(Constants.ApiErrorCodes.Unknown, problem.Errors[0].Code);
    }

    [Fact]
    public async Task ThrownApiErrorException_ReturnsProblemStatusAndCodes()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/throw/api-error", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(400, problem.Status);
        Assert.Equal(Constants.ApiErrorCodes.InvalidSelectField, problem.Errors[0].Code);
        Assert.Contains("Foo.Bar", problem.GetFullMessage());
        Assert.Equal("/throw/api-error", problem.Instance);
    }

    [Fact]
    public async Task ThrownApiErrorException_Forbidden_UsesProblemStatus()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/throw/api-error-forbidden", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(403, problem.Status);
        Assert.Equal(Constants.ApiErrorCodes.Forbidden, problem.Errors[0].Code);
    }

    [Fact]
    public async Task BareNotFoundResult_GetsFallbackProblemDetailsBody()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/bare/not-found", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(404, problem.Status);
        Assert.Equal(Constants.ApiErrorCodes.NotFound, problem.Errors[0].Code);
    }

    [Fact]
    public async Task BareUnauthorizedResult_GetsFallbackProblemDetailsBody()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/bare/unauthorized", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(401, problem.Status);
        Assert.Equal(Constants.ApiErrorCodes.Unauthorized, problem.Errors[0].Code);
    }

    [Fact]
    public async Task StartedUnauthorizedChallenge_StillReturns401()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/challenge/unauthorized", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        // Body may be empty when the challenge already started the response; logging still occurs in middleware.
    }

    [Fact]
    public async Task SuccessfulResponse_IsNotTouched()
    {
        using var host = await StartHostAsync();
        var response = await host.GetTestClient().GetAsync("/ok", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"value\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApiClient_SurfacesProblemAsApiExceptionWithErrorCode()
    {
        using var host = await StartHostAsync();
        var apiClient = new ApiClient(httpClient: host.GetTestClient());
        var ex = await Assert.ThrowsAsync<ApiException>(() => apiClient.GetAsAsync<object>("/throw/not-found", ct: TestContext.Current.CancellationToken));
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal(Constants.ApiErrorCodes.NotFound, ex.ErrorCode);
        Assert.False(ex.IsTransient);
        Assert.IsAssignableFrom<HttpException>(ex);
        Assert.NotNull(ex.ProblemDetails);
        Assert.Contains("Widget", ex.ProblemDetails.Detail);
    }
}