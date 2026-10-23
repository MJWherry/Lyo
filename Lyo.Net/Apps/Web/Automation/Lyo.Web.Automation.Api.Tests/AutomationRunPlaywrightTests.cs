using System.Net;
using System.Net.Http.Json;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Tests.Fixtures;

namespace Lyo.Web.Automation.Api.Tests;

public sealed class AutomationRunPlaywrightTests
{
    [Fact]
    public async Task Post_NavigateLocalPage_LogContainsRunStarted()
    {
        using var pages = new WebAutomationTestPageHostFixture();
        await using var host = await AutomationApiHost.StartPlaywrightAsync();
        var response = await host.Http.PostAsJsonAsync(
            Constants.Rest.Runs, new AutomationRunReq { Plan = new("local", [new NavigateAutomationStep(pages.HomeUri.AbsoluteUri)]), Headless = true }, AutomationApiHost.Json,
            TestCt);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var accepted = (await response.Content.ReadFromJsonAsync<AutomationRunAccepted>(AutomationApiHost.Json, TestCt))!;
        var started = DateTime.UtcNow;
        AutomationRunRes? run = null;
        while (DateTime.UtcNow - started < TimeSpan.FromSeconds(60)) {
            var body = await host.Http.GetFromJsonAsync<AutomationRunRes>(Constants.Rest.Run(accepted.RunId), AutomationApiHost.Json, TestCt);
            if (body?.Status is AutomationRunStatus.Completed or AutomationRunStatus.Failed or AutomationRunStatus.Cancelled) {
                run = body;
                break;
            }

            await Task.Delay(200, TestCt);
        }

        Assert.NotNull(run);
        Assert.Equal(AutomationRunStatus.Completed, run.Status);
        var log = await host.Http.GetStringAsync(Constants.Rest.Log(accepted.RunId), TestCt);
        Assert.Contains("RUN_STARTED", log, StringComparison.Ordinal);
    }
}