using Lyo.Exceptions.Models;
using Lyo.FeatureFlags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.FeatureFlags.Tests;

public sealed class FeatureFlagOptionsTests
{
    [Fact]
    public void Add_WithoutMode_StaysOff()
    {
        var service = Service(o => o.Add("checkout"));
        Assert.False(service.IsEnabled("checkout", FeatureFlagContext.Create().WithUser("u1").Build()));
    }

    [Fact]
    public void Add_FluentChain_EvaluatesOnPercentageAndRules()
    {
        var service = Service(o => o
            .Add("checkout").On()
            .Add("billing").Percentage(51)
            .Add("admin").WhenClaim("role", "admin")
            .Add("acme-admin").When(r => r.Tenant("acme").Claim("role", "admin").Claim("scope", "write")));

        Assert.True(service.IsEnabled("checkout"));
        Assert.True(service.IsEnabled("billing", FeatureFlagContext.Create().WithUser("user-a").Build()));
        Assert.True(service.IsEnabled("admin", FeatureFlagContext.Create().WithClaim("role", "admin").Build()));
        Assert.False(service.IsEnabled("acme-admin", FeatureFlagContext.Create().WithTenant("acme").WithClaim("role", "admin").Build()));
        Assert.True(service.IsEnabled("acme-admin", FeatureFlagContext.Create().WithTenant("acme").WithClaim("role", "admin").WithClaim("scope", "write").Build()));
    }

    [Fact]
    public void Add_DuplicateName_Throws()
    {
        var options = new FeatureFlagOptions();
        options.Add("Checkout").On();
        Assert.Throws<ArgumentException>(() => options.Add("checkout"));
    }

    [Fact]
    public void Add_WhenClaimThenWhenTenant_AreOr()
    {
        var service = Service(o => o.Add("gate").WhenClaim("role", "admin").WhenTenant("acme"));
        Assert.True(service.IsEnabled("gate", FeatureFlagContext.Create().WithClaim("role", "admin").Build()));
        Assert.True(service.IsEnabled("gate", FeatureFlagContext.Create().WithTenant("acme").Build()));
        Assert.False(service.IsEnabled("gate", FeatureFlagContext.Create().WithUser("other").Build()));
    }

    [Fact]
    public void Add_LastModeWins_ClearsPrevious()
    {
        var options = new FeatureFlagOptions();
        options.Add("a").On().Percentage(10).StickToTenant();
        Assert.Equal(FeatureFlagMode.Percentage, options.Flags["a"].Mode);
        Assert.Equal(10, options.Flags["a"].Percentage);
        Assert.Equal(FeatureFlagStickiness.Tenant, options.Flags["a"].Stickiness);
        Assert.Empty(options.Flags["a"].Rules);

        options.Add("b").Percentage(40).WhenClaim("role", "admin");
        Assert.Equal(FeatureFlagMode.Rules, options.Flags["b"].Mode);
        Assert.Null(options.Flags["b"].Percentage);
        Assert.Equal(FeatureFlagStickiness.User, options.Flags["b"].Stickiness);
        Assert.Null(options.Flags["b"].StickinessAttribute);
        Assert.Single(options.Flags["b"].Rules);

        options.Add("c").WhenClaim("role", "admin").On();
        Assert.Equal(FeatureFlagMode.On, options.Flags["c"].Mode);
        Assert.Empty(options.Flags["c"].Rules);
        Assert.Null(options.Flags["c"].Percentage);
    }

    [Fact]
    public void Add_LastStickinessWins()
    {
        var options = new FeatureFlagOptions();
        options.Add("d").Percentage(10).StickToTenant().StickToAttribute("install");
        Assert.Equal(FeatureFlagStickiness.Attribute, options.Flags["d"].Stickiness);
        Assert.Equal("install", options.Flags["d"].StickinessAttribute);
    }

    [Fact]
    public void FromConfiguration_BindsModesAndRules()
    {
        using var provider = ConfigProvider(new Dictionary<string, string?>
        {
            ["FeatureFlags:Flags:billing:Mode"] = "Percentage",
            ["FeatureFlags:Flags:billing:Percentage"] = "10",
            ["FeatureFlags:Flags:billing:Stickiness"] = "User",
            ["FeatureFlags:Flags:billing-install:Mode"] = "Percentage",
            ["FeatureFlags:Flags:billing-install:Percentage"] = "10",
            ["FeatureFlags:Flags:billing-install:Stickiness"] = "Attribute",
            ["FeatureFlags:Flags:billing-install:StickinessAttribute"] = "install",
            ["FeatureFlags:Flags:admin:Mode"] = "Rules",
            ["FeatureFlags:Flags:admin:Rules:0:TenantId"] = "acme",
            ["FeatureFlags:Flags:admin:Rules:0:Claims:0:Type"] = "role",
            ["FeatureFlags:Flags:admin:Rules:0:Claims:0:Value"] = "admin",
            ["FeatureFlags:Flags:admin:Rules:0:Claims:1:Type"] = "scope",
            ["FeatureFlags:Flags:admin:Rules:0:Claims:1:Value"] = "write",
            ["FeatureFlags:Flags:admin:Rules:0:Attributes:0:Key"] = "plan",
            ["FeatureFlags:Flags:admin:Rules:0:Attributes:0:Value"] = "pro"
        });

        var install = provider.GetRequiredService<FeatureFlagOptions>().Flags["billing-install"];
        Assert.Equal(FeatureFlagStickiness.Attribute, install.Stickiness);
        Assert.Equal("install", install.StickinessAttribute);

        var service = provider.GetRequiredService<IFeatureFlagService>();
        Assert.False(service.IsEnabled("billing", FeatureFlagContext.Create().WithUser("user-a").Build()));
        var match = FeatureFlagContext.Create().WithTenant("acme").WithClaim("role", "admin").WithClaim("scope", "write").WithAttribute("plan", "pro").Build();
        Assert.True(service.IsEnabled("admin", match));
        Assert.False(service.IsEnabled("admin", FeatureFlagContext.Create().WithTenant("acme").WithClaim("role", "admin").Build()));
    }

    [Fact]
    public void FromConfiguration_MissingSection_AllDisabled()
    {
        using var provider = ConfigProvider([]);
        Assert.False(provider.GetRequiredService<IFeatureFlagService>().IsEnabled("checkout"));
    }

    [Fact]
    public void FromConfiguration_FlagNameDiffersByCase_ReturnsTrue()
    {
        using var provider = ConfigProvider(new Dictionary<string, string?> { ["FeatureFlags:Flags:Checkout:Mode"] = "On" });
        Assert.True(provider.GetRequiredService<IFeatureFlagService>().IsEnabled("checkout"));
    }

    [Fact]
    public void FromConfiguration_UnknownMode_Throws()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FeatureFlags:Flags:billing:Mode"] = "Percent" }).Build();
        var ex = Assert.Throws<ConfigurationException>(() => new ServiceCollection().AddFeatureFlagsFromConfiguration(configuration));
        Assert.Equal("billing", ex.SettingName);
    }

    [Fact]
    public void Add_SecondRegistration_KeepsFirst()
    {
        var services = new ServiceCollection();
        services.AddFeatureFlags(o => o.Add("a").On());
        services.AddFeatureFlags(o => o.Add("b").On());
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IFeatureFlagService>();
        Assert.True(service.IsEnabled("a"));
        Assert.False(service.IsEnabled("b"));
        Assert.True(provider.GetRequiredService<IOptions<FeatureFlagOptions>>().Value.Flags.ContainsKey("a"));
    }

    [Fact]
    public void Validate_PercentageMissingOrOutOfRange_Throws()
    {
        Assert.Throws<ArgumentException>(() => Flag(new FeatureFlagDefinition { Mode = FeatureFlagMode.Percentage }).Validate());
        Assert.Throws<ArgumentException>(() => Flag(new FeatureFlagDefinition { Mode = FeatureFlagMode.Percentage, Percentage = -1 }).Validate());
        Assert.Throws<ArgumentException>(() => Flag(new FeatureFlagDefinition { Mode = FeatureFlagMode.Percentage, Percentage = 101 }).Validate());
    }

    [Fact]
    public void Validate_RulesWithoutRules_Throws() =>
        Assert.Throws<ArgumentException>(() => Flag(new FeatureFlagDefinition { Mode = FeatureFlagMode.Rules }).Validate());

    [Fact]
    public void Validate_EmptyRule_Throws() =>
        Assert.Throws<ArgumentException>(() => Flag(new FeatureFlagDefinition { Mode = FeatureFlagMode.Rules, Rules = [new FeatureFlagRule()] }).Validate());

    [Fact]
    public void Validate_HalfSetClaim_Throws() =>
        Assert.Throws<ArgumentException>(() => Flag(new FeatureFlagDefinition
        {
            Mode = FeatureFlagMode.Rules,
            Rules = [new FeatureFlagRule { Claims = [new FeatureFlagClaim { Type = "role" }] }]
        }).Validate());

    [Fact]
    public void Validate_HalfSetAttribute_Throws() =>
        Assert.Throws<ArgumentException>(() => Flag(new FeatureFlagDefinition
        {
            Mode = FeatureFlagMode.Rules,
            Rules = [new FeatureFlagRule { Attributes = [new FeatureFlagAttribute { Key = "plan" }] }]
        }).Validate());

    [Fact]
    public void Validate_ModeOffWithRules_Throws() =>
        Assert.Throws<ArgumentException>(() => Flag(new FeatureFlagDefinition
        {
            Mode = FeatureFlagMode.Off,
            Rules = [new FeatureFlagRule { UserId = "u1" }]
        }).Validate());

    [Fact]
    public void Validate_StickinessAttributeBlank_Throws() =>
        Assert.Throws<ArgumentException>(() => Flag(new FeatureFlagDefinition
        {
            Mode = FeatureFlagMode.Percentage,
            Percentage = 10,
            Stickiness = FeatureFlagStickiness.Attribute,
            StickinessAttribute = " "
        }).Validate());

    [Fact]
    public void Validate_CaseDuplicateKeys_Throws()
    {
        var options = new FeatureFlagOptions
        {
            Flags = new Dictionary<string, FeatureFlagDefinition>(StringComparer.Ordinal)
            {
                ["Checkout"] = new() { Mode = FeatureFlagMode.On },
                ["checkout"] = new() { Mode = FeatureFlagMode.On }
            }
        };
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    private static FeatureFlagService Service(Action<FeatureFlagOptions> configure)
    {
        var options = new FeatureFlagOptions();
        configure(options);
        return new FeatureFlagService(options);
    }

    private static FeatureFlagOptions Flag(FeatureFlagDefinition definition)
    {
        var options = new FeatureFlagOptions();
        options.Flags["flag"] = definition;
        return options;
    }

    private static ServiceProvider ConfigProvider(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddFeatureFlagsFromConfiguration(configuration);
        return services.BuildServiceProvider();
    }
}
