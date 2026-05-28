using Lyo.Authentication.Exceptions;
using Lyo.Authentication.Scopes;

namespace Lyo.Authentication.Tests;

public class ScopeRegistryTests
{
    [Fact]
    public void Register_RecordsScopeAndDescription()
    {
        var r = new ScopeRegistry();
        r.Register("people.read", "Read");
        Assert.True(r.IsRegistered("people.read"));
        Assert.Equal("Read", r.TryGet("people.read")!.Description);
    }

    [Fact]
    public void Expand_ResolvesTransitiveImplies()
    {
        var r = new ScopeRegistry();
        r.Register("people.read", "Read");
        r.Register("people.write", "Write", "people.read");
        r.Register("admin", "Admin", "people.write");
        var set = r.Expand(["admin"]);
        Assert.Contains("admin", set);
        Assert.Contains("people.write", set);
        Assert.Contains("people.read", set);
    }

    [Fact]
    public void Expand_UnknownScope_Throws()
    {
        var r = new ScopeRegistry();
        Assert.Throws<ScopeNotRegisteredException>(() => r.Expand(new[] { "ghost" }));
    }

    [Fact]
    public void Register_InvalidName_Throws()
    {
        var r = new ScopeRegistry();
        Assert.Throws<ArgumentException>(() => r.Register("Bad Name", "x"));
    }

    [Fact]
    public void All_PreservesRegistrationOrder()
    {
        var r = new ScopeRegistry();
        r.Register("a.read", "");
        r.Register("b.read", "");
        r.Register("c.read", "");
        var names = r.All.Select(s => s.Name).ToArray();
        Assert.Equal(new[] { "a.read", "b.read", "c.read" }, names);
    }
}