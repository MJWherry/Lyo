using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Email.Tests;

public class EmailServiceExtensionsTests
{
    [Fact]
    public void AddEmailService_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddEmailService(_ => new() {
            Host = "smtp.example.com",
            Port = 587,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        }));
    }

    [Fact]
    public void AddEmailService_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddEmailService(null!));
    }

    [Fact]
    public void AddEmailService_WithFunc_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddEmailService(_ => new() {
            Host = "smtp.example.com",
            Port = 587,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        });

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<EmailService>();
        var interfaceService = provider.GetService<IEmailService>();
        Assert.NotNull(service);
        Assert.NotNull(interfaceService);
        Assert.Same(service, interfaceService);
    }

    [Fact]
    public void AddEmailService_WithAction_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddEmailService(options => {
            options.Host = "smtp.example.com";
            options.Port = 587;
            options.DefaultFromAddress = "test@example.com";
            options.DefaultFromName = "Test";
        });

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<EmailService>();
        var interfaceService = provider.GetService<IEmailService>();
        Assert.NotNull(service);
        Assert.NotNull(interfaceService);
    }

    [Fact]
    public void AddEmailService_WithActionAndProvider_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddEmailService((_, options) => {
            options.Host = "smtp.example.com";
            options.Port = 587;
            options.DefaultFromAddress = "test@example.com";
            options.DefaultFromName = "Test";
        });

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<EmailService>();
        var interfaceService = provider.GetService<IEmailService>();
        Assert.NotNull(service);
        Assert.NotNull(interfaceService);
    }

    [Fact]
    public void AddEmailService_InvalidOptions_ThrowsOnResolve()
    {
        var services = new ServiceCollection();
        services.AddEmailService(_ => new() {
            Host = "", // Invalid
            Port = 587,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        });

        var provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<EmailService>());
    }

    [Fact]
    public void AddEmailServiceFromConfiguration_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        var config = new ConfigurationBuilder().Build();
        Assert.Throws<ArgumentNullException>(() => services!.AddEmailServiceFromConfiguration(config));
    }

    [Fact]
    public void AddEmailServiceFromConfiguration_EmptySectionName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        Assert.Throws<ArgumentException>(() => services.AddEmailServiceFromConfiguration(config, ""));
    }

    [Fact]
    public void AddEmailServiceFromConfiguration_WhitespaceSectionName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        Assert.Throws<ArgumentException>(() => services.AddEmailServiceFromConfiguration(config, "   "));
    }

    [Fact]
    public void AddEmailServiceFromConfiguration_WithValidConfig_RegistersServices()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    { "EmailServiceOptions:Host", "smtp.example.com" },
                    { "EmailServiceOptions:Port", "587" },
                    { "EmailServiceOptions:DefaultFromAddress", "test@example.com" },
                    { "EmailServiceOptions:DefaultFromName", "Test" }
                })
            .Build();

        var services = new ServiceCollection();
        services.AddEmailServiceFromConfiguration(config);
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<EmailService>();
        var interfaceService = provider.GetService<IEmailService>();
        Assert.NotNull(service);
        Assert.NotNull(interfaceService);
    }

    [Fact]
    public void AddEmailServiceFromConfiguration_WithCustomSectionName_RegistersServices()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    { "CustomEmail:Host", "smtp.example.com" },
                    { "CustomEmail:Port", "587" },
                    { "CustomEmail:DefaultFromAddress", "test@example.com" },
                    { "CustomEmail:DefaultFromName", "Test" }
                })
            .Build();

        var services = new ServiceCollection();
        services.AddEmailServiceFromConfiguration(config, "CustomEmail");
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<EmailService>();
        var interfaceService = provider.GetService<IEmailService>();
        Assert.NotNull(service);
        Assert.NotNull(interfaceService);
    }

    [Fact]
    public void AddEmailService_RegistersLogger_WhenAvailable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmailService(options => {
            options.Host = "smtp.example.com";
            options.Port = 587;
            options.DefaultFromAddress = "test@example.com";
            options.DefaultFromName = "Test";
        });

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<EmailService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddEmailService_RegistersMetrics_WhenAvailable()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMetrics>(NullMetrics.Instance);
        services.AddEmailService(options => {
            options.Host = "smtp.example.com";
            options.Port = 587;
            options.DefaultFromAddress = "test@example.com";
            options.DefaultFromName = "Test";
            options.EnableMetrics = true;
        });

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<EmailService>();
        Assert.NotNull(service);
    }
}