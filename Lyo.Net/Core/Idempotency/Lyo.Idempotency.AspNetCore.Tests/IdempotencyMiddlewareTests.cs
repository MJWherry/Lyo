using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Idempotency.AspNetCore.Tests;

public sealed class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task TwoPrincipals_SameKey_BothRun()
    {
        var runs = 0;
        await using var host = await HostAsync(app => app.MapPost(
            "/pay", () => {
                runs++;
                return Results.Ok("paid");
            }));

        var first = await SendAsync(host, "/pay", "k", "a", "user-a");
        var second = await SendAsync(host, "/pay", "k", "a", "user-b");
        Assert.Equal(StatusCodes.Status200OK, first.Status);
        Assert.Equal(StatusCodes.Status200OK, second.Status);
        Assert.Equal(2, runs);
    }

    [Fact]
    public async Task MissingSubject_WithHeader_Is400()
    {
        await using var host = await HostAsync(app => app.MapPost("/pay", () => Results.Ok()));
        var response = await SendAsync(host, "/pay", "k", "a");
        Assert.Equal(StatusCodes.Status400BadRequest, response.Status);
        Assert.Contains("idempotency_subject_required", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_WithHeader_RunsEveryCall()
    {
        var runs = 0;
        await using var host = await HostAsync(app => app.MapGet(
            "/read", () => {
                runs++;
                return Results.Ok("x");
            }));

        Assert.Equal(StatusCodes.Status200OK, (await SendAsync(host, "/read", "k", method: HttpMethods.Get, lyoUser: "user-a")).Status);
        Assert.Equal(StatusCodes.Status200OK, (await SendAsync(host, "/read", "k", method: HttpMethods.Get, lyoUser: "user-a")).Status);
        Assert.Equal(2, runs);
    }

    [Fact]
    public async Task DifferentJsonBodies_Conflict_HandlerRunsOnce()
    {
        var runs = 0;
        await using var host = await HostAsync(app => app.MapPost(
            "/json", (IdempotencyNote note) => {
                runs++;
                return Results.Ok(note.Name);
            }));

        var first = await SendAsync(host, "/json", "k", """{"name":"a"}""", "user-a", contentType: "application/json");
        var second = await SendAsync(host, "/json", "k", """{"name":"b"}""", "user-a", contentType: "application/json");
        Assert.Equal(StatusCodes.Status200OK, first.Status);
        Assert.Equal(StatusCodes.Status409Conflict, second.Status);
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task TypedResultsJson_Replays201()
    {
        var runs = 0;
        await using var host = await HostAsync(app => app.MapPost(
            "/created", () => {
                runs++;
                return TypedResults.Json(new { ok = true }, statusCode: StatusCodes.Status201Created);
            }));

        var first = await SendAsync(host, "/created", "k", "{}", "user-a", contentType: "application/json");
        var second = await SendAsync(host, "/created", "k", "{}", "user-a", contentType: "application/json");
        Assert.Equal(StatusCodes.Status201Created, first.Status);
        Assert.Equal(StatusCodes.Status201Created, second.Status);
        Assert.Contains("true", second.Body, StringComparison.Ordinal);
        Assert.Equal("true", second.Headers["Idempotency-Replayed"]);
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task EndpointThrow_Replays500WithoutRunningAgain()
    {
        var runs = 0;
        await using var host = await HostAsync(app => app.MapPost(
            "/boom", () => {
                runs++;
                throw new InvalidOperationException("nope");
            }));

        var first = await SendAsync(host, "/boom", "k", "a", "user-a");
        var second = await SendAsync(host, "/boom", "k", "a", "user-a");
        Assert.Equal(StatusCodes.Status500InternalServerError, first.Status);
        Assert.Equal(StatusCodes.Status500InternalServerError, second.Status);
        Assert.Equal("true", second.Headers["Idempotency-Replayed"]);
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task InProgress_Is409()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host = await HostAsync(app => app.MapPost(
            "/slow", async () => {
                started.TrySetResult();
                await release.Task;
                return Results.Ok("done");
            }));

        var first = SendAsync(host, "/slow", "k", "a", "user-a");
        await started.Task.WaitAsync(TestCt);
        var second = await SendAsync(host, "/slow", "k", "a", "user-a");
        release.TrySetResult();
        await first;
        Assert.Equal(StatusCodes.Status409Conflict, second.Status);
    }

    [Fact]
    public async Task RequireIdempotencyKey_OnAction_OnClass_AndMinimalApi_Returns400WithoutHeader()
    {
        await using var host = await HostAsync(
            app => {
                app.MapControllers();
                app.MapPost("/mini", () => Results.Ok("m")).WithMetadata(new RequireIdempotencyKeyAttribute());
            }, true);

        Assert.Equal(StatusCodes.Status400BadRequest, (await SendAsync(host, "/ctrl/action", lyoUser: "user-a")).Status);
        Assert.Equal(StatusCodes.Status400BadRequest, (await SendAsync(host, "/ctrl/class", lyoUser: "user-a")).Status);
        Assert.Equal(StatusCodes.Status400BadRequest, (await SendAsync(host, "/mini", lyoUser: "user-a")).Status);
        Assert.Equal(StatusCodes.Status200OK, (await SendAsync(host, "/ctrl/action", "k", "a", "user-a")).Status);
    }

    private static async Task<WebApplication> HostAsync(Action<WebApplication> map, bool controllers = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IIdempotencyStore>(new InMemoryIdempotencyStore());
        if (controllers)
            builder.Services.AddControllers().AddApplicationPart(typeof(KeyedController).Assembly);

        var app = builder.Build();
        app.UseRouting();
        app.Use(async (context, next) => {
            var claims = new List<Claim>();
            var lyo = context.Request.Headers["X-Test-Lyo-User"].ToString();
            var sub = context.Request.Headers["X-Test-Sub"].ToString();
            if (!string.IsNullOrWhiteSpace(lyo))
                claims.Add(new("lyo:user", lyo));

            if (!string.IsNullOrWhiteSpace(sub))
                claims.Add(new("sub", sub));

            if (claims.Count > 0)
                context.User = new(new ClaimsIdentity(claims, "test"));

            await next(context);
        });

        app.UseIdempotency();
        map(app);
        await app.StartAsync(TestCt);
        return app;
    }

    private static async Task<Captured> SendAsync(
        WebApplication app,
        string path,
        string? key = null,
        string? body = null,
        string? lyoUser = null,
        string? sub = null,
        string method = "POST",
        string? contentType = null)
    {
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(new(method), path);
        if (key is not null)
            request.Headers.TryAddWithoutValidation("Idempotency-Key", key);

        if (lyoUser is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Lyo-User", lyoUser);

        if (sub is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Sub", sub);

        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "text/plain");

        var response = await client.SendAsync(request, TestCt);
        var text = await response.Content.ReadAsStringAsync(TestCt);
        return new((int)response.StatusCode, text, response.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault() ?? string.Empty, StringComparer.OrdinalIgnoreCase));
    }

    private sealed record Captured(int Status, string Body, Dictionary<string, string> Headers);
}

public sealed record IdempotencyNote(string Name);

[ApiController]
[Route("ctrl")]
public sealed class KeyedController : ControllerBase
{
    [HttpPost("action")]
    [RequireIdempotencyKey]
    public IActionResult Action() => Ok("paid");
}

[ApiController]
[Route("ctrl")]
[RequireIdempotencyKey]
public sealed class ClassKeyedController : ControllerBase
{
    [HttpPost("class")]
    public IActionResult Unmarked() => Ok("paid");
}