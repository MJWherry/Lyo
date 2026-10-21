using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Postgres.Tests;

public sealed class PostgresAutomationPlanStoreTests(AutomationPlanPostgresFixture fixture)
{
    [Fact]
    public async Task Create_GetById_QueryByName_ReturnsDocumentAndStepCount()
    {
        var store = fixture.ServiceProvider.GetRequiredService<IAutomationPlanStore>();
        var name = "Checkout-" + Guid.NewGuid().ToString("N");
        var created = store.Create(name);
        Assert.Equal(name, created.Name);
        Assert.Empty(created.Steps);
        var saved = new AutomationPlan(name, [new NavigateAutomationStep("https://example.com/"), new DelayAutomationStep(10)]);
        Assert.True(store.TryReplace(name, saved));
        var id = store.List().Single(item => item.Name == name).Id;
        await using var db = await fixture.ContextFactory.CreateDbContextAsync(TestCt);
        var row = await db.Plans.AsNoTracking().SingleAsync(plan => plan.Name == name, TestCt);
        var loaded = store.GetById(id);
        Assert.Equal(2, loaded!.Steps.Count);
        Assert.IsType<NavigateAutomationStep>(loaded.Steps[0]);
        Assert.Equal(2, row.StepCount);
        Assert.Contains("navigate", row.Document, StringComparison.Ordinal);
    }
}