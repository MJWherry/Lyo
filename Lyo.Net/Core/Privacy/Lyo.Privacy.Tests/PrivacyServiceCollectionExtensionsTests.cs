using Lyo.Privacy.Abstractions;
using Lyo.Privacy.AspNetCore;
using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Privacy.Tests;

public sealed class PrivacyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLyoPrivacy_registers_redactors()
    {
        var services = new ServiceCollection();
        services.AddLyoPrivacy(null, o => o.DefaultPreset = PrivacyPresetNames.Logging, b => b.AddRule(new LiteralSubstringRedactionRule("XYZZY")));
        using var sp = services.BuildServiceProvider();
        var text = sp.GetRequiredService<ITextRedactor>();
        Assert.Contains("[redacted]", text.Redact("XYZZY").Text);
        _ = sp.GetRequiredService<IStructuredRedactor>();
    }

    [Fact]
    public void AddLyoPrivacy_binds_configuration_section()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Privacy:DefaultPreset"] = PrivacyPresetNames.Minimal, ["Privacy:Placeholder"] = "***" })
            .Build();

        var services = new ServiceCollection();
        services.AddLyoPrivacy(cfg);
        using var sp = services.BuildServiceProvider();
        var text = sp.GetRequiredService<ITextRedactor>();
        var res = text.Redact("anything");
        Assert.Equal("anything", res.Text);
    }

    [Fact]
    public void AddLyoPrivacyPolicy_keyed_resolver()
    {
        var services = new ServiceCollection();
        services.AddLyoPrivacy();
        services.AddLyoPrivacyPolicy("strict", b => b.AddPolicy(PrivacyPolicies.PublicSurface()));
        using var sp = services.BuildServiceProvider();
        var keyed = sp.GetRequiredKeyedService<ITextRedactor>("strict");
        var res = keyed.Redact("203.0.113.9");
        Assert.Equal("[redacted]", res.Text);
    }
}