using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class ConfigurationExceptionTests
{
    [Fact]
    public void DefaultCtor_HasDefaultMessage_AndNoSettingName()
    {
        var ex = new ConfigurationException();
        Assert.Contains("misconfigured", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(ex.SettingName);
    }

    [Fact]
    public void MessageCtor_PreservesMessage()
    {
        var ex = new ConfigurationException("Missing connection string.");
        Assert.Equal("Missing connection string.", ex.Message);
        Assert.Null(ex.SettingName);
    }

    [Fact]
    public void MessageAndInnerCtor_PreservesBoth()
    {
        var inner = new IOException("disk");
        var ex = new ConfigurationException("Missing connection string.", inner);
        Assert.Equal("Missing connection string.", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void SettingNameCtor_ExposesSettingName()
    {
        var ex = new ConfigurationException("Redis connection string not found.", "RedisLock");
        Assert.Equal("RedisLock", ex.SettingName);
        Assert.Equal("Redis connection string not found.", ex.Message);
    }

    [Fact]
    public void SettingNameAndInnerCtor_ExposesAll()
    {
        var inner = new IOException("disk");
        var ex = new ConfigurationException("Bad key material.", "Encryption:KeyPath", inner);
        Assert.Equal("Encryption:KeyPath", ex.SettingName);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ToString_IncludesSettingName_WhenSet()
    {
        var ex = new ConfigurationException("Bad value.", "My:Setting");
        Assert.Contains("My:Setting", ex.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ToString_OmitsSettingSuffix_WhenNotSet()
    {
        var ex = new ConfigurationException("Bad value.");
        Assert.DoesNotContain("(Setting:", ex.ToString(), StringComparison.Ordinal);
    }
}
