using System.Globalization;
using System.Text;
using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class FormatHelpersTests
{
    [Fact]
    public void ThrowIfInvalidGuid_Valid_DoesNotThrow() => FormatHelpers.ThrowIfInvalidGuid("550e8400-e29b-41d4-a716-446655440000");

    [Fact]
    public void ThrowIfInvalidGuid_Invalid_ThrowsWithInvalidValue()
    {
        var ex = Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfInvalidGuid("not-a-guid"));
        Assert.Equal("not-a-guid", ex.InvalidValue);
        Assert.Contains("Invalid GUID format", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfInvalidGuid_Null_ThrowsArgumentNull()
    {
        string? id = null;
        Assert.Throws<ArgumentNullException>(() => FormatHelpers.ThrowIfInvalidGuid(id));
    }

    [Fact]
    public void GetValidGuid_Valid_ReturnsParsedGuid()
    {
        var expected = Guid.NewGuid();
        Assert.Equal(expected, FormatHelpers.GetValidGuid(expected.ToString()));
    }

    [Fact]
    public void GetValidGuid_Invalid_Throws() => Assert.Throws<InvalidFormatException>(() => FormatHelpers.GetValidGuid("nope"));

    [Theory]
    [InlineData("#FFF")]
    [InlineData("FFF")]
    [InlineData("#a1b2c3")]
    [InlineData("a1b2c3")]
    public void ThrowIfInvalidHexColor_Valid_DoesNotThrow(string value) => FormatHelpers.ThrowIfInvalidHexColor(value);

    [Theory]
    [InlineData("#FFFF")]
    [InlineData("#GGG")]
    [InlineData("red")]
    public void ThrowIfInvalidHexColor_Invalid_Throws(string value) => Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfInvalidHexColor(value));

    [Fact]
    public void GetValidHexColor_WithoutHash_ReturnsPrefixed() => Assert.Equal("#A1B2C3", FormatHelpers.GetValidHexColor("A1B2C3"));

    [Fact]
    public void GetValidHexColor_WithHash_ReturnsUnchanged() => Assert.Equal("#FFF", FormatHelpers.GetValidHexColor("#FFF"));

    [Fact]
    public void ThrowIfInvalidBase64_Valid_DoesNotThrow() => FormatHelpers.ThrowIfInvalidBase64("aGVsbG8=");

    [Fact]
    public void ThrowIfInvalidBase64_Invalid_Throws() => Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfInvalidBase64("!!!not base64!!!"));

    [Fact]
    public void GetValidBase64_Valid_ReturnsDecodedBytes() => Assert.Equal("hello", Encoding.UTF8.GetString(FormatHelpers.GetValidBase64("aGVsbG8=")));

    [Fact]
    public void GetValidBase64_Invalid_Throws() => Assert.Throws<InvalidFormatException>(() => FormatHelpers.GetValidBase64("!!!"));

    [Fact]
    public void ThrowIfInvalidDateTime_Valid_DoesNotThrow() => FormatHelpers.ThrowIfInvalidDateTime("2024-01-15", formatProvider: CultureInfo.InvariantCulture);

    [Fact]
    public void ThrowIfInvalidDateTime_Invalid_Throws()
        => Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfInvalidDateTime("not-a-date", formatProvider: CultureInfo.InvariantCulture));

    [Fact]
    public void GetValidDateTime_Valid_ReturnsParsedValue()
        => Assert.Equal(new(2024, 1, 15), FormatHelpers.GetValidDateTime("2024-01-15", formatProvider: CultureInfo.InvariantCulture));

    [Fact]
    public void GetValidDateTime_Invalid_Throws()
        => Assert.Throws<InvalidFormatException>(() => FormatHelpers.GetValidDateTime("later", formatProvider: CultureInfo.InvariantCulture));

    [Fact]
    public void ThrowIfInvalidFormat_Matching_DoesNotThrow() => FormatHelpers.ThrowIfInvalidFormat("abc-123", new("^[a-z0-9-]+$"), "Invalid slug: {0}");

    [Fact]
    public void ThrowIfInvalidFormat_NotMatching_ThrowsWithFormattedMessage()
    {
        var ex = Assert.Throws<InvalidFormatException>(()
            => FormatHelpers.ThrowIfInvalidFormat("ABC", new("^[a-z]+$"), "Invalid slug: {0}", validFormats: "Lowercase letters only"));

        Assert.Contains("Invalid slug: ABC", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Lowercase letters only", ex.ValidFormats);
    }

    [Fact]
    public void ThrowIfNotAlphanumeric_Valid_DoesNotThrow() => FormatHelpers.ThrowIfNotAlphanumeric("abc123");

    [Theory]
    [InlineData("abc 123")]
    [InlineData("abc-1")]
    public void ThrowIfNotAlphanumeric_Invalid_Throws(string value) => Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfNotAlphanumeric(value));

    [Fact]
    public void ThrowIfNotAlpha_Valid_DoesNotThrow() => FormatHelpers.ThrowIfNotAlpha("abcDEF");

    [Fact]
    public void ThrowIfNotAlpha_Digits_Throws() => Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfNotAlpha("abc1"));

    [Fact]
    public void ThrowIfNotNumeric_Valid_DoesNotThrow() => FormatHelpers.ThrowIfNotNumeric("0123456789");

    [Fact]
    public void ThrowIfNotNumeric_Letters_Throws() => Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfNotNumeric("123a"));

    [Fact]
    public void ThrowIfContainsWhitespace_NoWhitespace_DoesNotThrow() => FormatHelpers.ThrowIfContainsWhitespace("abc");

    [Fact]
    public void ThrowIfContainsWhitespace_Whitespace_Throws() => Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfContainsWhitespace("a b"));

    [Fact]
    public void ThrowIfContainsWhitespace_Empty_ThrowsArgument() => Assert.Throws<ArgumentException>(() => FormatHelpers.ThrowIfContainsWhitespace(""));

    [Fact]
    public void ThrowIfInvalidLength_InRange_DoesNotThrow() => FormatHelpers.ThrowIfInvalidLength("abc", 1, 5);

    [Theory]
    [InlineData("", 1, 5)]
    [InlineData("abcdef", 1, 5)]
    public void ThrowIfInvalidLength_OutOfRange_Throws(string value, int min, int max)
        => Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfInvalidLength(value, min, max));

    [Theory]
    [InlineData(1)]
    [InlineData(22)]
    [InlineData(65535)]
    public void IsValidPort_Valid_ReturnsTrue(int port) => Assert.True(FormatHelpers.IsValidPort(port));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void IsValidPort_Invalid_ReturnsFalse(int port) => Assert.False(FormatHelpers.IsValidPort(port));

    [Fact]
    public void ThrowIfInvalidPort_Valid_DoesNotThrow() => FormatHelpers.ThrowIfInvalidPort(443);

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void ThrowIfInvalidPort_Invalid_ThrowsWithDetails(int port)
    {
        var ex = Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfInvalidPort(port));
        Assert.Equal(port.ToString(CultureInfo.InvariantCulture), ex.InvalidValue);
        Assert.Contains("1-65535", ex.ValidFormats);
    }

    [Fact]
    public void ThrowIfNotInRange_InRange_DoesNotThrow() => FormatHelpers.ThrowIfNotInRange(4, 1);

    [Fact]
    public void ThrowIfNotInRange_MinOnly_OutOfRange_ThrowsWithExpectedFormat()
    {
        var ex = Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfNotInRange(0, 1));
        Assert.Equal("0", ex.InvalidValue);
        Assert.Contains(">= 1", ex.ValidFormats);
    }

    [Fact]
    public void ThrowIfNotInRange_Bounded_OutOfRange_Throws()
    {
        var ex = Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIfNotInRange(11, 1, 10));
        Assert.Contains("1-10", ex.ValidFormats);
    }

    [Fact]
    public void ThrowIf_False_DoesNotThrow() => FormatHelpers.ThrowIf(false, "unused");

    [Fact]
    public void ThrowIf_True_ThrowsWithDetails()
    {
        var ex = Assert.Throws<InvalidFormatException>(() => FormatHelpers.ThrowIf(true, "Bad format.", "field", "xyz", "abc-style"));
        Assert.Equal("field", ex.ParamName);
        Assert.Equal("xyz", ex.InvalidValue);
        Assert.Equal(["abc-style"], ex.ValidFormats);
    }
}