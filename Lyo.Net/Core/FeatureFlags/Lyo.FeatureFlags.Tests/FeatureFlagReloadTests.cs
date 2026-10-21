using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.FeatureFlags.Tests;

public sealed class FeatureFlagReloadTests
{
    [Fact]
    public void TryApply_ValidDocument_IsEnabledSeesNewFlag()
    {
        var services = new ServiceCollection();
        services.AddFeatureFlags(o => o.Add("a").On());
        using var provider = services.BuildServiceProvider();
        var updater = provider.GetRequiredService<IFeatureFlagUpdater>();
        var next = new FeatureFlagOptions();
        next.Add("b").On();
        string? changed = null;
        provider.GetRequiredService<IOptionsMonitor<FeatureFlagOptions>>().OnChange((options, _) => changed = options.Flags.Keys.Single());
        Assert.True(updater.TryApply(next));
        var service = provider.GetRequiredService<IFeatureFlagService>();
        Assert.False(service.IsEnabled("a"));
        Assert.True(service.IsEnabled("b"));
        Assert.Equal("b", changed);
        Assert.True(provider.GetRequiredService<IOptionsMonitor<FeatureFlagOptions>>().CurrentValue.Flags.ContainsKey("b"));
        Assert.True(provider.GetRequiredService<IOptions<FeatureFlagOptions>>().Value.Flags.ContainsKey("a"));
        Assert.False(provider.GetRequiredService<IOptions<FeatureFlagOptions>>().Value.Flags.ContainsKey("b"));
    }

    [Fact]
    public void TryApply_InvalidDocument_KeepsPrevious()
    {
        var services = new ServiceCollection();
        services.AddFeatureFlags(o => o.Add("a").On());
        using var provider = services.BuildServiceProvider();
        var updater = provider.GetRequiredService<IFeatureFlagUpdater>();
        var fired = false;
        provider.GetRequiredService<IOptionsMonitor<FeatureFlagOptions>>().OnChange((_, _) => fired = true);
        var bad = new FeatureFlagOptions { Flags = new() { ["x"] = new() { Mode = FeatureFlagMode.Percentage } } };
        Assert.False(updater.TryApply(bad));
        Assert.True(provider.GetRequiredService<IFeatureFlagService>().IsEnabled("a"));
        Assert.False(fired);
    }

    [Fact]
    public void FromConfiguration_Reload_FlipsFlag()
    {
        var source = new ReloadableConfigurationSource(new() { ["FeatureFlags:Flags:sample:Mode"] = "Off" });
        var configuration = new ConfigurationBuilder().Add(source).Build();
        var services = new ServiceCollection();
        services.AddFeatureFlagsFromConfiguration(configuration);
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IFeatureFlagService>();
        Assert.False(service.IsEnabled("sample"));
        source.Provider.Set(new() { ["FeatureFlags:Flags:sample:Mode"] = "On" });
        Assert.True(service.IsEnabled("sample"));
        source.Provider.Set(new() { ["FeatureFlags:Flags:other:Mode"] = "On" });
        Assert.False(service.IsEnabled("sample"));
        Assert.True(service.IsEnabled("other"));
    }

    [Fact]
    public void FromConfiguration_BadReload_KeepsPrevious()
    {
        var source = new ReloadableConfigurationSource(new() { ["FeatureFlags:Flags:sample:Mode"] = "On" });
        var configuration = new ConfigurationBuilder().Add(source).Build();
        var services = new ServiceCollection();
        services.AddFeatureFlagsFromConfiguration(configuration);
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IFeatureFlagService>();
        source.Provider.Set(new() { ["FeatureFlags:Flags:sample:Mode"] = "Percent" });
        Assert.True(service.IsEnabled("sample"));
    }

    [Fact]
    public void AddFeatureFlags_Action_DoesNotWatchConfiguration()
    {
        var source = new ReloadableConfigurationSource(new() { ["FeatureFlags:Flags:a:Mode"] = "On" });
        var configuration = new ConfigurationBuilder().Add(source).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddFeatureFlags(o => o.Add("a").On());
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IFeatureFlagService>();
        source.Provider.Set(new() { ["FeatureFlags:Flags:a:Mode"] = "Off", ["FeatureFlags:Flags:b:Mode"] = "On" });
        Assert.True(service.IsEnabled("a"));
        Assert.False(service.IsEnabled("b"));
    }

    [Fact]
    public void WithEnabled_On_ReplacesThatFlagAndKeepsOthers()
    {
        var options = new FeatureFlagOptions();
        options.Add("billing").Percentage(10);
        options.Add("sample");
        options.Validate();
        var next = FeatureFlagUpdate.WithEnabled(options, "sample", true);
        Assert.NotNull(next);
        Assert.Equal(FeatureFlagMode.On, next.Flags["sample"].Mode);
        Assert.Equal(FeatureFlagMode.Percentage, next.Flags["billing"].Mode);
        Assert.Equal(10, next.Flags["billing"].Percentage);
        Assert.Equal(FeatureFlagMode.Off, options.Flags["sample"].Mode);
    }

    [Fact]
    public void WithEnabled_Off_ClearsPercentageAndRules()
    {
        var options = new FeatureFlagOptions();
        options.Add("gated").WhenUser("user-a");
        options.Validate();
        var next = FeatureFlagUpdate.WithEnabled(options, "gated", false);
        Assert.NotNull(next);
        var flag = next.Flags["gated"];
        Assert.Equal(FeatureFlagMode.Off, flag.Mode);
        Assert.Null(flag.Percentage);
        Assert.Empty(flag.Rules);
    }

    [Fact]
    public void WithEnabled_UnknownName_ReturnsNull()
    {
        var options = new FeatureFlagOptions();
        options.Add("sample").On();
        options.Validate();
        Assert.Null(FeatureFlagUpdate.WithEnabled(options, "missing", true));
        Assert.Equal(FeatureFlagMode.On, options.Flags["sample"].Mode);
    }

    [Fact]
    public void WithEnabled_DoesNotMutatePrevious()
    {
        var options = new FeatureFlagOptions();
        options.Add("billing").Percentage(10).StickToTenant();
        options.Add("gated").WhenUser("user-a");
        options.Validate();
        var next = FeatureFlagUpdate.WithEnabled(options, "billing", true);
        Assert.NotNull(next);
        Assert.Equal(FeatureFlagMode.Percentage, options.Flags["billing"].Mode);
        Assert.Equal(10, options.Flags["billing"].Percentage);
        Assert.Equal(FeatureFlagStickiness.Tenant, options.Flags["billing"].Stickiness);
        Assert.Equal(FeatureFlagMode.Rules, options.Flags["gated"].Mode);
        Assert.Equal("user-a", options.Flags["gated"].Rules[0].UserId);
        Assert.NotSame(options.Flags["gated"], next.Flags["gated"]);
        Assert.NotSame(options.Flags["gated"].Rules, next.Flags["gated"].Rules);
    }

    private sealed class ReloadableConfigurationSource(Dictionary<string, string?> initial) : IConfigurationSource
    {
        public ReloadableConfigurationProvider Provider { get; } = new(initial);

        public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;
    }

    private sealed class ReloadableConfigurationProvider : ConfigurationProvider
    {
        public ReloadableConfigurationProvider(Dictionary<string, string?> initial) => Replace(initial);

        public void Set(Dictionary<string, string?> data)
        {
            Replace(data);
            OnReload();
        }

        private void Replace(Dictionary<string, string?> data)
        {
            var next = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in data)
                next[pair.Key] = pair.Value;

            Data = next;
        }
    }
}