using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Tests;

public sealed class AutomationPlanInterpolationTests
{
    [Fact]
    public async Task ExpandAsync_DoubleBrace_IsNotRewritten()
    {
        var ct = TestCt;
        var bindings = Bindings();
        var expanded = await AutomationPlanInterpolation.ExpandAsync("visit {{page.url}}", bindings, null, ct);
        Assert.Equal("visit {{page.url}}", expanded);
    }

    [Fact]
    public async Task ExpandAsync_SingleBrace_ResolvesStringVariable()
    {
        var ct = TestCt;
        var expanded = await AutomationPlanInterpolation.ExpandAsync("Hello {name}", Bindings(), null, ct);
        Assert.Equal("Hello Ada", expanded);
    }

    [Fact]
    public void Expand_SingleBrace_ResolvesStringVariable()
    {
        var strings = new Dictionary<string, string> { ["name"] = "Ada" };
        Assert.Equal("Hello Ada", AutomationPlanInterpolation.Expand("Hello {name}", strings));
        Assert.Equal("Hello {{name}}", AutomationPlanInterpolation.Expand("Hello {{name}}", strings));
    }

    private static AutomationPlanInterpolationBindings Bindings()
        => new() {
            Strings = new Dictionary<string, string> { ["name"] = "Ada" },
            StringLists = new Dictionary<string, IReadOnlyList<string>>(),
            Elements = new Dictionary<string, IWebAutomationElement>(),
            ContextItems = new Dictionary<string, object?>()
        };
}