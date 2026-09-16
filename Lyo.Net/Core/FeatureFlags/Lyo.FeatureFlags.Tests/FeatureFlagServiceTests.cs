using Lyo.FeatureFlags;

namespace Lyo.FeatureFlags.Tests;

public sealed class FeatureFlagServiceTests
{
    [Fact]
    public void IsEnabled_UnknownName_ReturnsFalse()
    {
        var service = Service(_ => { });
        Assert.False(service.IsEnabled("missing"));
    }

    [Fact]
    public void IsEnabled_ModeOff_ReturnsFalse()
    {
        var service = Service(o => o.Flags["off"] = new FeatureFlagDefinition { Mode = FeatureFlagMode.Off });
        Assert.False(service.IsEnabled("off", User("user-a")));
    }

    [Fact]
    public void IsEnabled_ModeOn_IgnoresContext_ReturnsTrue()
    {
        var service = Service(o => o.Add("on").On());
        Assert.True(service.IsEnabled("on"));
        Assert.True(service.IsEnabled("on", FeatureFlagContext.Create().WithStickiness("nope").WithUser("user-a").Build()));
    }

    [Fact]
    public void IsEnabled_WhitespaceName_Throws()
    {
        var service = Service(_ => { });
        Assert.Throws<ArgumentException>(() => service.IsEnabled(" "));
        Assert.Throws<ArgumentNullException>(() => service.IsEnabled(null!));
    }

    [Fact]
    public void IsEnabled_NameDiffersByCase_ReturnsTrue()
    {
        var service = Service(o => o.Add("Checkout").On());
        Assert.True(service.IsEnabled("checkout"));
    }

    [Fact]
    public void Bucket_BillingUserA_Is50() => Assert.Equal(50, FeatureFlagHash.Bucket("billing", "user-a"));

    [Fact]
    public void IsEnabled_Percentage_UsesBucket()
    {
        var user = User("user-a");
        Assert.False(Service(o => o.Add("billing").Percentage(0)).IsEnabled("billing", user));
        Assert.True(Service(o => o.Add("billing").Percentage(100)).IsEnabled("billing"));
        Assert.True(Service(o => o.Add("billing").Percentage(51)).IsEnabled("billing", user));
        Assert.False(Service(o => o.Add("billing").Percentage(50)).IsEnabled("billing", user));
        Assert.False(Service(o => o.Add("billing").Percentage(51)).IsEnabled("billing"));

        var stable = Service(o => o.Add("billing").Percentage(51));
        Assert.True(stable.IsEnabled("billing", user));
        Assert.True(stable.IsEnabled("billing", user));
    }

    [Fact]
    public void IsEnabled_Percentage_StickToTenant_IgnoresUserId()
    {
        var context = FeatureFlagContext.Create().WithUser("user-a").WithTenant("tenant-1").Build();
        var service = Service(o => o.Add("rollout").Percentage(53).StickToTenant());
        Assert.False(service.IsEnabled("rollout", context));
        Assert.True(Service(o => o.Add("rollout").Percentage(90).StickToTenant()).IsEnabled("rollout", context));
    }

    [Fact]
    public void IsEnabled_Percentage_StickToAttribute_MissingKey_ReturnsFalse()
    {
        var context = FeatureFlagContext.Create().WithUser("user-a").Build();
        var service = Service(o => o.Add("rollout").Percentage(53).StickToAttribute("install"));
        Assert.False(service.IsEnabled("rollout", context));
    }

    [Fact]
    public void IsEnabled_Percentage_StickinessKey_OverridesFlag()
    {
        var context = FeatureFlagContext.Create().WithUser("user-a").WithStickiness("override-key").Build();
        var service = Service(o => o.Add("billing").Percentage(30));
        Assert.True(service.IsEnabled("billing", context));
        Assert.False(service.IsEnabled("billing", User("user-a")));
    }

    [Fact]
    public void IsEnabled_Rules_UserTenantClaimAttribute_Match()
    {
        var service = Service(o => o.Flags["gate"] = RuleFlag(
            new FeatureFlagRule { UserId = "u1" },
            new FeatureFlagRule { TenantId = "acme" },
            new FeatureFlagRule { Claims = [new FeatureFlagClaim { Type = "role", Value = "admin" }] },
            new FeatureFlagRule { Attributes = [new FeatureFlagAttribute { Key = "plan", Value = "pro" }] }));

        Assert.True(service.IsEnabled("gate", User("u1")));
        Assert.False(service.IsEnabled("gate", User("other")));
        Assert.True(service.IsEnabled("gate", FeatureFlagContext.Create().WithTenant("acme").Build()));
        Assert.False(service.IsEnabled("gate", FeatureFlagContext.Create().WithTenant("other").Build()));
        Assert.True(service.IsEnabled("gate", FeatureFlagContext.Create().WithClaim("role", "user").WithClaim("role", "admin").Build()));
        Assert.False(service.IsEnabled("gate", FeatureFlagContext.Create().WithClaim("role", "user").Build()));
        Assert.True(service.IsEnabled("gate", FeatureFlagContext.Create().WithAttribute("plan", "free").WithAttribute("plan", "pro").Build()));
        Assert.False(service.IsEnabled("gate", FeatureFlagContext.Create().WithAttribute("plan", "free").Build()));
    }

    [Fact]
    public void IsEnabled_Rules_AndWithinRule_RequiresAll()
    {
        var service = Service(o => o.Add("both").When(r => r.Tenant("acme").Claim("role", "admin")));
        Assert.False(service.IsEnabled("both", FeatureFlagContext.Create().WithTenant("acme").Build()));
        Assert.False(service.IsEnabled("both", FeatureFlagContext.Create().WithClaim("role", "admin").Build()));
        Assert.True(service.IsEnabled("both", FeatureFlagContext.Create().WithTenant("acme").WithClaim("role", "admin").Build()));
    }

    [Fact]
    public void IsEnabled_Rules_OrAcrossRules_AnyMatch()
    {
        var service = Service(o => o.Flags["either"] = RuleFlag(
            new FeatureFlagRule { UserId = "u1" },
            new FeatureFlagRule { TenantId = "acme" }));
        Assert.True(service.IsEnabled("either", User("u1")));
        Assert.True(service.IsEnabled("either", FeatureFlagContext.Create().WithTenant("acme").Build()));
        Assert.False(service.IsEnabled("either", User("other")));
    }

    [Fact]
    public void IsEnabled_Rules_NoContext_ReturnsFalse()
    {
        var service = Service(o => o.Add("admin").WhenClaim("role", "admin"));
        Assert.False(service.IsEnabled("admin"));
    }

    [Fact]
    public void IsEnabled_Rules_MissingAttribute_ReturnsFalse()
    {
        var service = Service(o => o.Add("plan").WhenAttribute("plan", "pro"));
        Assert.False(service.IsEnabled("plan", FeatureFlagContext.Create().WithAttribute("tier", "pro").Build()));
    }

    [Fact]
    public void IsEnabled_OptionsMutatedAfterConstruct_KeepsSnapshot()
    {
        var options = new FeatureFlagOptions();
        options.Add("checkout").On();
        var service = new FeatureFlagService(options);
        options.Flags["checkout"].Mode = FeatureFlagMode.Off;
        Assert.True(service.IsEnabled("checkout"));
    }

    private static FeatureFlagService Service(Action<FeatureFlagOptions> configure)
    {
        var options = new FeatureFlagOptions();
        configure(options);
        return new FeatureFlagService(options);
    }

    private static FeatureFlagContext User(string id) => FeatureFlagContext.Create().WithUser(id).Build();

    private static FeatureFlagDefinition RuleFlag(params FeatureFlagRule[] rules) => new()
    {
        Mode = FeatureFlagMode.Rules,
        Rules = [.. rules]
    };
}
