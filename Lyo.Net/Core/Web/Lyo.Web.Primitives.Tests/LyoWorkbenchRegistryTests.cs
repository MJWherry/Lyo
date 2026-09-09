using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives.Tests;

public sealed class LyoWorkbenchRegistryTests
{
    [Fact]
    public void Find_MatchingSlug_ReturnsDescriptor()
    {
        var registry = new LyoWorkbenchRegistry([new LyoWorkbenchDescriptor("Schedule", "", "Infrastructure", "schedule", typeof(Stub))]);
        Assert.Equal("Schedule", registry.Find("schedule")?.Title);
    }

    [Fact]
    public void Find_UnknownSlug_ReturnsNull()
    {
        var registry = new LyoWorkbenchRegistry([]);
        Assert.Null(registry.Find("missing"));
    }

    private sealed class Stub : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}
