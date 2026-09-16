using System.Net;
using System.Net.Http.Json;
using System.Text;
using Lyo.Common.Core.Identifiers;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Api.Tests;

public sealed class AutomationRunRouteTests
{
    [Fact]
    public async Task Post_MissingPlan_Returns400()
    {
        await using var host = await AutomationApiHost.StartScriptedAsync(hold: false);
        using var body = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await host.Http.PostAsync(Constants.Rest.Runs, body, TestCt);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WhileRunActive_Returns429()
    {
        await using var host = await AutomationApiHost.StartScriptedAsync(hold: true);

        var first = await PostNavigateAsync(host.Http);
        var second = await PostNavigateAsync(host.Http);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownRun_Returns404()
    {
        await using var host = await AutomationApiHost.StartScriptedAsync(hold: false);

        var response = await host.Http.GetAsync(Constants.Rest.Run(LyoGuid.CreateV7()), TestCt);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSnapshot_ParentPath_Returns400()
    {
        await using var host = await AutomationApiHost.StartScriptedAsync(hold: false);

        var response = await host.Http.GetAsync($"{Constants.Rest.Runs}/{LyoGuid.CreateV7()}/Snapshot/foo..bar.png", TestCt);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhileRunning_ReturnsCompletedSteps()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await using var host = await AutomationApiHost.StartReportingAsync(new SeededStorage(png));

        var accepted = await ReadAcceptedAsync(await PostNavigateAsync(host.Http));
        var run = await WaitForStepAsync(host.Http, accepted.RunId);

        Assert.Equal(AutomationRunStatus.Running, run.Status);
        var step = Assert.Single(run.Steps);
        Assert.Equal(0, step.Index);
        Assert.Equal("Success", step.Outcome);
        Assert.Equal("snap-1", step.ScreenshotFileId);
        Assert.NotNull(step.Context);
        Assert.Equal("hello", step.Context.Strings["greeting"]);

        var shot = await host.Http.GetByteArrayAsync(Constants.Rest.StepScreenshot(accepted.RunId, 0), TestCt);
        Assert.Equal(png, shot);
        await host.Http.PostAsync(Constants.Rest.Cancel(accepted.RunId), null, TestCt);
    }

    [Fact]
    public async Task GetLog_CompletedRun_ReturnsTranscript()
    {
        await using var host = await AutomationApiHost.StartScriptedAsync(hold: false, transcript: "RUN_STARTED plan=demo\n");

        var accepted = await ReadAcceptedAsync(await PostNavigateAsync(host.Http));
        await WaitForStatusAsync(host.Http, accepted.RunId, AutomationRunStatus.Completed);
        var log = await host.Http.GetStringAsync(Constants.Rest.Log(accepted.RunId), TestCt);

        Assert.Contains("RUN_STARTED plan=demo", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancel_RunningRun_StatusCancelled()
    {
        await using var host = await AutomationApiHost.StartScriptedAsync(hold: true);

        var accepted = await ReadAcceptedAsync(await PostNavigateAsync(host.Http));
        var cancel = await host.Http.PostAsync(Constants.Rest.Cancel(accepted.RunId), null, TestCt);
        var run = await WaitForStatusAsync(host.Http, accepted.RunId, AutomationRunStatus.Cancelled);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(AutomationRunStatus.Cancelled, run.Status);
    }

    private static Task<HttpResponseMessage> PostNavigateAsync(HttpClient http)
        => http.PostAsJsonAsync(Constants.Rest.Runs, new AutomationRunReq { Plan = new AutomationPlan("demo", [new NavigateAutomationStep("https://example.com/")]) }, AutomationApiHost.Json, TestCt);

    private static async Task<AutomationRunAccepted> ReadAcceptedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AutomationRunAccepted>(AutomationApiHost.Json, TestCt))!;
    }

    private static async Task<AutomationRunRes> WaitForStatusAsync(HttpClient http, Guid runId, string status)
    {
        var started = DateTime.UtcNow;
        while (DateTime.UtcNow - started < TimeSpan.FromSeconds(5)) {
            var response = await http.GetAsync(Constants.Rest.Run(runId), TestCt);
            response.EnsureSuccessStatusCode();
            var run = (await response.Content.ReadFromJsonAsync<AutomationRunRes>(AutomationApiHost.Json, TestCt))!;
            if (run.Status == status)
                return run;

            await Task.Delay(25, TestCt);
        }

        throw new TimeoutException($"Run {runId} did not reach {status}.");
    }

    private static async Task<AutomationRunRes> WaitForStepAsync(HttpClient http, Guid runId)
    {
        var started = DateTime.UtcNow;
        while (DateTime.UtcNow - started < TimeSpan.FromSeconds(5)) {
            var response = await http.GetAsync(Constants.Rest.Run(runId), TestCt);
            response.EnsureSuccessStatusCode();
            var run = (await response.Content.ReadFromJsonAsync<AutomationRunRes>(AutomationApiHost.Json, TestCt))!;
            if (run.Steps.Count > 0)
                return run;

            await Task.Delay(25, TestCt);
        }

        throw new TimeoutException($"Run {runId} did not report a step.");
    }

    private sealed class SeededStorage(byte[] png) : IAutomationPlanFileStorage
    {
        public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> SaveSnapshotAsync(byte[] pngBytes, Guid runId, Guid stepId, CancellationToken ct) => Task.FromResult<string?>("snap-1");

        public Task<string?> SaveAsync(byte[] data, string fileName, string pathPrefix, string contentType, CancellationToken ct) => Task.FromResult<string?>(null);

        public Task<byte[]?> ReadSnapshotAsync(string fileId, CancellationToken ct) => Task.FromResult<byte[]?>(fileId == "snap-1" ? png : null);
    }
}
