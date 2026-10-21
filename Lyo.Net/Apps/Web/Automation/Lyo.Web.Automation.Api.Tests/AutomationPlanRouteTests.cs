using System.Net;
using System.Net.Http.Json;
using System.Text;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Api.Tests;

public sealed class AutomationPlanRouteTests
{
    [Fact]
    public async Task Create_ThenListAndGet_ReturnsTheEmptyPlan()
    {
        await using var host = await AutomationApiHost.StartScriptedAsync(false);
        var created = await host.Http.PostAsJsonAsync("Automation/Plan", new PlanNameReq("Checkout"), AutomationApiHost.Json, TestCt);
        var plan = await ReadPlanAsync(created, HttpStatusCode.Created);
        Assert.Equal("Checkout", plan.Name);
        Assert.Empty(plan.Steps);
        var listed = await host.Http.GetFromJsonAsync<PlanNameRes[]>("Automation/Plan", AutomationApiHost.Json, TestCt);
        Assert.Contains(listed!, item => item.Name == "Checkout");
        var got = await host.Http.GetFromJsonAsync<AutomationPlan>("Automation/Plan/Checkout", AutomationApiHost.Json, TestCt);
        Assert.Equal("Checkout", got!.Name);
        Assert.Empty(got.Steps);
    }

    [Fact]
    public async Task Put_InvalidJson_LeavesTheStoredPlan()
    {
        await using var host = await AutomationApiHost.StartScriptedAsync(false);
        await ReadPlanAsync(await host.Http.PostAsJsonAsync("Automation/Plan", new PlanNameReq("Checkout"), AutomationApiHost.Json, TestCt), HttpStatusCode.Created);
        using var body = new StringContent("{", Encoding.UTF8, "application/json");
        var put = await host.Http.PutAsync("Automation/Plan/Checkout", body, TestCt);
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        var got = await host.Http.GetFromJsonAsync<AutomationPlan>("Automation/Plan/Checkout", AutomationApiHost.Json, TestCt);
        Assert.Equal("Checkout", got!.Name);
        Assert.Empty(got.Steps);
    }

    [Fact]
    public async Task Post_SlashName_Returns400()
    {
        await using var host = await AutomationApiHost.StartScriptedAsync(false);
        var response = await host.Http.PostAsJsonAsync("Automation/Plan", new PlanNameReq("nested/Checkout"), AutomationApiHost.Json, TestCt);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<AutomationPlan> ReadPlanAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AutomationPlan>(AutomationApiHost.Json, TestCt))!;
    }

    private sealed record PlanNameReq(string Name);

    private sealed record PlanNameRes(string Name);
}