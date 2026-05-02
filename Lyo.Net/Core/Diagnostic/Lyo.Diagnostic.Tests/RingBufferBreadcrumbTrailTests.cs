using Lyo.Diagnostic.Breadcrumbs;

namespace Lyo.Diagnostic.Tests;

public sealed class RingBufferBreadcrumbTrailTests
{
    [Fact]
    public void Add_DropsOldest_WhenOverCapacity()
    {
        IBreadcrumbTrail trail = new RingBufferBreadcrumbTrail(2);
        trail.Add("a", "1");
        trail.Add("b", "2");
        trail.Add("c", "3");
        var snap = trail.Snapshot();
        Assert.Equal(2, snap.Count);
        Assert.Equal("2", snap[0].Message);
        Assert.Equal("3", snap[1].Message);
    }

    [Fact]
    public void Redactor_TransformsBeforeStore()
    {
        var redactor = new PrefixRedactor();
        IBreadcrumbTrail trail = new RingBufferBreadcrumbTrail(10, redactor);
        trail.Add("x", "hi");
        Assert.Equal("X:hi", trail.Snapshot()[0].Message);
    }

    private sealed class PrefixRedactor : IBreadcrumbRedactor
    {
        public Breadcrumb Redact(Breadcrumb breadcrumb) => breadcrumb with { Message = "X:" + breadcrumb.Message };
    }
}
