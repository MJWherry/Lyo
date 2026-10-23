using Lyo.Common.Metadata.Records;
using Lyo.Email.Smtp;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Email.Tests;

public class EmailServiceExtensionsTests
{
    [Fact]
    public void AddSmtpEmailService_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddSmtpEmailService(_ => Valid(new())));
    }

    [Fact]
    public void AddSmtpEmailService_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddSmtpEmailService((Action<SmtpEmailOptions>)null!));
    }

    [Fact]
    public void AddSmtpEmailService_Action_RegistersTheSameInstanceForTheInterface()
    {
        var services = new ServiceCollection();
        services.AddSmtpEmailService(Valid);
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<SmtpEmailService>();
        Assert.Same(service, provider.GetRequiredService<IEmailService>());
        Assert.Equal("smtp.example.com", provider.GetRequiredService<SmtpEmailOptions>().Host);
        Assert.NotNull(provider.GetRequiredService<IOptions<SmtpEmailOptions>>().Value);
    }

    [Fact]
    public void AddSmtpEmailService_Instance_RegistersServices()
    {
        var services = new ServiceCollection();
        var options = new SmtpEmailOptions();
        Valid(options);
        services.AddSmtpEmailService(options);
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IEmailService>());
    }

    [Fact]
    public void AddSmtpEmailService_InvalidOptions_ThrowsAtRegistration()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddSmtpEmailService(options => {
            options.Host = "";
            options.Port = PortInfo.SmtpSubmission;
            options.DefaultFromAddress = "test@example.com";
            options.DefaultFromName = "Test";
        }));

        Assert.Equal("Host", ex.ParamName);
    }

    [Fact]
    public void AddSmtpEmailServiceFromConfiguration_EmptySectionName_Throws()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        Assert.Throws<ArgumentException>(() => services.AddSmtpEmailServiceFromConfiguration(config, "   "));
    }

    [Fact]
    public void AddSmtpEmailServiceFromConfiguration_BindsTheSection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    ["SmtpEmailOptions:Host"] = "smtp.example.com",
                    ["SmtpEmailOptions:Port"] = "587",
                    ["SmtpEmailOptions:DefaultFromAddress"] = "test@example.com",
                    ["SmtpEmailOptions:DefaultFromName"] = "Test"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddSmtpEmailServiceFromConfiguration(config);
        using var provider = services.BuildServiceProvider();
        Assert.IsType<SmtpEmailService>(provider.GetRequiredService<IEmailService>());
    }

    [Fact]
    public void AddSmtpEmailServiceFromConfiguration_CustomSection_BindsThatSection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    ["CustomEmail:Host"] = "smtp.example.com",
                    ["CustomEmail:Port"] = "2525",
                    ["CustomEmail:DefaultFromAddress"] = "test@example.com",
                    ["CustomEmail:DefaultFromName"] = "Test"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddSmtpEmailServiceFromConfiguration(config, "CustomEmail");
        using var provider = services.BuildServiceProvider();
        Assert.Equal(2525, provider.GetRequiredService<SmtpEmailOptions>().Port);
    }

    [Fact]
    public void AddSmtpEmailService_ResolvesWhenMetricsAreRegistered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMetrics>(NullMetrics.Instance);
        services.AddSmtpEmailService(options => {
            Valid(options);
            options.EnableMetrics = true;
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<SmtpEmailService>());
    }

    private static void Valid(SmtpEmailOptions options)
    {
        options.Host = "smtp.example.com";
        options.Port = PortInfo.SmtpSubmission;
        options.DefaultFromAddress = "test@example.com";
        options.DefaultFromName = "Test";
    }
}
