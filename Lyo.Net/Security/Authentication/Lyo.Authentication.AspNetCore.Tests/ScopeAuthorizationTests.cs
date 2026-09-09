using Lyo.Authentication.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Lyo.Authentication.AspNetCore.Tests;

public sealed class ScopeAuthorizationTests
{
    [Fact]
    public async Task PolicyProvider_BuildsScopePolicy()
    {
        var provider = new ScopeAuthorizationPolicyProvider(Microsoft.Extensions.Options.Options.Create(new AuthorizationOptions()));
        var policy = await provider.GetPolicyAsync("scope:people.read");
        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, r => r is ScopeAuthorizationRequirement req && req.Scope == "people.read");
    }

    [Fact]
    public async Task PolicyProvider_FallsBackForUnknownPolicies()
    {
        var provider = new ScopeAuthorizationPolicyProvider(Microsoft.Extensions.Options.Options.Create(new AuthorizationOptions()));
        var policy = await provider.GetPolicyAsync("not-a-scope-policy");
        Assert.Null(policy);
    }

    [Fact]
    public async Task PolicyProvider_RejectsEmptyScope()
    {
        var provider = new ScopeAuthorizationPolicyProvider(Microsoft.Extensions.Options.Options.Create(new AuthorizationOptions()));
        var policy = await provider.GetPolicyAsync("scope:");
        Assert.Null(policy);
    }
}