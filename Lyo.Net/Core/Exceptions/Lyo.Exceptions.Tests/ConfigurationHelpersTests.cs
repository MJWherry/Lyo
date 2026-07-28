using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class ConfigurationHelpersTests
{
    [Fact]
    public void ThrowIf_False_DoesNotThrow() => ConfigurationHelpers.ThrowIf(false, "should not throw");

    [Fact]
    public void ThrowIf_True_ThrowsWithMessage()
    {
        var ex = Assert.Throws<ConfigurationException>(() => ConfigurationHelpers.ThrowIf(true, "No scanner configured."));
        Assert.Equal("No scanner configured.", ex.Message);
        Assert.Null(ex.SettingName);
    }

    [Fact]
    public void ThrowIf_True_WithSettingName_ExposesSettingName()
    {
        var ex = Assert.Throws<ConfigurationException>(() => ConfigurationHelpers.ThrowIf(true, "No scanner configured.", "RequireScanBeforeAvailable"));
        Assert.Equal("RequireScanBeforeAvailable", ex.SettingName);
    }

    [Fact]
    public void ThrowIfNull_NonNull_DoesNotThrow() => ConfigurationHelpers.ThrowIfNull(new());

    [Fact]
    public void ThrowIfNull_Null_ThrowsWithCallerExpressionAsSettingName()
    {
        object? connectionString = null;
        var ex = Assert.Throws<ConfigurationException>(() => ConfigurationHelpers.ThrowIfNull(connectionString));
        Assert.Equal("connectionString", ex.SettingName);
        Assert.Contains("connectionString", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfNull_Null_WithCustomMessage_UsesMessage()
    {
        object? value = null;
        var ex = Assert.Throws<ConfigurationException>(() => ConfigurationHelpers.ThrowIfNull(value, "Custom message."));
        Assert.Equal("Custom message.", ex.Message);
        Assert.Equal("value", ex.SettingName);
    }

    [Fact]
    public void ThrowIfNullOrWhiteSpace_Value_DoesNotThrow() => ConfigurationHelpers.ThrowIfNullOrWhiteSpace("configured");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ThrowIfNullOrWhiteSpace_Invalid_Throws(string? value)
    {
        var ex = Assert.Throws<ConfigurationException>(() => ConfigurationHelpers.ThrowIfNullOrWhiteSpace(value));
        Assert.Equal("value", ex.SettingName);
        Assert.Contains("value", ex.Message, StringComparison.Ordinal);
    }
}