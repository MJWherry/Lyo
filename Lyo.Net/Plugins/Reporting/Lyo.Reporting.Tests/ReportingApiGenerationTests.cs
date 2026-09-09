using System.Security.Claims;
using Lyo.Api.Models.Error;
using Lyo.Reporting.Api;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using Microsoft.AspNetCore.Http;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Reporting.Tests;

/// <summary>
/// Covers the two hardening fixes on the Generate endpoint: the server-side generation timeout cannot be reported as a client cancel, and a caller cannot be able to
/// attribute a generation to another user.
/// </summary>
public sealed class ReportingApiGenerationTests
{
    [Fact]
    public async Task MapException_GenerationTimeout_Returns504()
    {
        // The request is still alive; only the service-side GenerationTimeout fired.
        var error = await Assert.ThrowsAsync<ApiErrorException>(
            () => Extensions.ExecuteGenerationAsync(() => throw new OperationCanceledException(), CancellationToken.None));

        Assert.Equal(504, error.Status);
        Assert.Equal(ApiErrorCodes.GatewayTimeout, error.ProblemDetails.Errors[0].Code);
    }

    [Fact]
    public async Task MapException_CallerDisconnect_Returns499()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var error = await Assert.ThrowsAsync<ApiErrorException>(
            () => Extensions.ExecuteGenerationAsync(() => throw new OperationCanceledException(), aborted.Token));

        Assert.Equal(499, error.Status);
        Assert.Equal(ApiErrorCodes.Cancelled, error.ProblemDetails.Errors[0].Code);
    }

    [Fact]
    public async Task MapException_ValidationAndBusy_Returns400And503()
    {
        var validation = await Assert.ThrowsAsync<ApiErrorException>(
            () => Extensions.ExecuteGenerationAsync(() => throw new ReportValidationException("bad parameter"), CancellationToken.None));

        var busy = await Assert.ThrowsAsync<ApiErrorException>(
            () => Extensions.ExecuteGenerationAsync(() => throw new ReportBusyException("saturated"), CancellationToken.None));

        Assert.Equal(400, validation.Status);
        Assert.Equal(503, busy.Status);
    }

    [Fact]
    public async Task MapException_Unexpected_Bubbles()
        => await Assert.ThrowsAsync<InvalidOperationException>(
            () => Extensions.ExecuteGenerationAsync(() => throw new InvalidOperationException("boom"), CancellationToken.None));

    [Fact]
    public async Task ExecuteAsync_Success_ReturnsGeneration()
    {
        var generation = new ReportGenerationRes(
            Guid.NewGuid(), null, null, ReportFormat.Csv, ReportGenerationStatus.Succeeded, null, null, null, null, null, "tester", DateTime.UtcNow, null, null);

        var result = await Extensions.ExecuteGenerationAsync(() => Task.FromResult(generation), CancellationToken.None);
        Assert.NotNull(result);
    }

    [Fact]
    public void ResolveCreatedBy_AuthenticatedIdentity_OverridesSupplied()
    {
        var req = new GenerateReportReq { CreatedBy = "admin" };
        Extensions.StampCreatedBy(req, HttpContextFor("alice"), new());
        Assert.Equal("alice", req.CreatedBy);
    }

    [Fact]
    public void ResolveCreatedBy_Anonymous_DiscardsByDefault()
    {
        var req = new GenerateReportReq { CreatedBy = "admin" };
        Extensions.StampCreatedBy(req, HttpContextFor(null), new());
        Assert.Equal("Anonymous", req.CreatedBy);
    }

    [Fact]
    public void ResolveCreatedBy_AnonymousOptIn_KeepsValue()
    {
        var req = new GenerateReportReq { CreatedBy = "batch-runner" };
        Extensions.StampCreatedBy(req, HttpContextFor(null), new() { AllowAnonymousCreatedBy = true });
        Assert.Equal("batch-runner", req.CreatedBy);
    }

    [Fact]
    public void ResolveCreatedBy_Missing_FallsBackToAnonymous()
    {
        var req = new GenerateReportReq();
        Extensions.StampCreatedBy(req, HttpContextFor(null), new() { AllowAnonymousCreatedBy = true });
        Assert.Equal("Anonymous", req.CreatedBy);
    }

    private static DefaultHttpContext HttpContextFor(string? name)
        => new() {
            User = name is null
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "TestAuth"))
        };
}
