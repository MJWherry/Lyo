using Lyo.Api.ApiEndpoint.Config;

namespace Lyo.Api.Tests;

public sealed class PatchPropertyAuthorizationTests
{
    [Fact]
    public void TryGetForbiddenPropertyKeys_UnionOfSucceedingPolicies_AllowsUnion()
    {
        var policyMap = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal) {
            ["User"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name", "Email" },
            ["Admin"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "RoleId" }
        };

        var policySucceeded = new Dictionary<string, bool>(StringComparer.Ordinal) { ["User"] = true, ["Admin"] = false };

        var requestProps = new Dictionary<string, object?> { ["Name"] = "x", ["Email"] = "y" };

        var hasForbidden = PatchPropertyAuthorizationApplier.TryGetForbiddenPropertyKeys(policyMap, policySucceeded, requestProps, out var forbidden);

        Assert.False(hasForbidden);
        Assert.Empty(forbidden);
    }

    [Fact]
    public void TryGetForbiddenPropertyKeys_PropertyNotInUnion_IsForbidden()
    {
        var policyMap = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal) {
            ["User"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name" }
        };

        var policySucceeded = new Dictionary<string, bool>(StringComparer.Ordinal) { ["User"] = true };

        var requestProps = new Dictionary<string, object?> { ["Name"] = "a", ["RoleId"] = 1 };

        var hasForbidden = PatchPropertyAuthorizationApplier.TryGetForbiddenPropertyKeys(policyMap, policySucceeded, requestProps, out var forbidden);

        Assert.True(hasForbidden);
        Assert.Equal(["RoleId"], forbidden);
    }

    [Fact]
    public void TryGetForbiddenPropertyKeys_WildcardInSucceedingPolicy_AllowsAllRequestKeys()
    {
        var policyMap = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal) {
            ["Editor"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" }
        };

        var policySucceeded = new Dictionary<string, bool>(StringComparer.Ordinal) { ["Editor"] = true };

        var requestProps = new Dictionary<string, object?> { ["Anything"] = 1, ["Else"] = 2 };

        var hasForbidden = PatchPropertyAuthorizationApplier.TryGetForbiddenPropertyKeys(policyMap, policySucceeded, requestProps, out var forbidden);

        Assert.False(hasForbidden);
        Assert.Empty(forbidden);
    }

    [Fact]
    public void TryGetForbiddenPropertyKeys_NoPolicySucceeds_EverythingForbidden()
    {
        var policyMap = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal) {
            ["User"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name" }
        };

        var policySucceeded = new Dictionary<string, bool>(StringComparer.Ordinal) { ["User"] = false };

        var requestProps = new Dictionary<string, object?> { ["Name"] = "a" };

        var hasForbidden = PatchPropertyAuthorizationApplier.TryGetForbiddenPropertyKeys(policyMap, policySucceeded, requestProps, out var forbidden);

        Assert.True(hasForbidden);
        Assert.Equal(["Name"], forbidden);
    }

    [Fact]
    public void PatchPropertyAuthorizationBuilder_MergesSamePolicy()
    {
        var auth = PatchPropertyAuthorization.ForPolicies(b => b.AllowPropertiesForPolicy("P", "A").AllowPropertiesForPolicy("P", "B"));

        Assert.NotNull(auth.PolicyAllowedProperties);
        Assert.True(auth.PolicyAllowedProperties!.TryGetValue("P", out var set));
        Assert.Contains("A", set);
        Assert.Contains("B", set);
    }
}
