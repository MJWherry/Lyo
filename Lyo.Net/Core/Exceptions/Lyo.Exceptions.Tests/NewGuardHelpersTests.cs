using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class NewGuardHelpersTests
{
    [Fact]
    public void ThrowIfEmpty_NonEmptyGuid_DoesNotThrow() => ArgumentHelpers.ThrowIfEmpty(Guid.NewGuid());

    [Fact]
    public void ThrowIfEmpty_EmptyGuid_ThrowsWithCallerExpressionAsParamName()
    {
        var tenantId = Guid.Empty;
        var ex = Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfEmpty(tenantId));
        Assert.Equal("tenantId", ex.ParamName);
    }

    [Fact]
    public void ThrowIfDefault_NonDefault_DoesNotThrow()
    {
        ArgumentHelpers.ThrowIfDefault(new DateTime(2024, 6, 1));
        ArgumentHelpers.ThrowIfDefault(42);
    }

    [Fact]
    public void ThrowIfDefault_DefaultDateTime_Throws()
    {
        var createdAt = default(DateTime);
        var ex = Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfDefault(createdAt));
        Assert.Equal("createdAt", ex.ParamName);
        Assert.Contains("DateTime", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfDefault_ZeroInt_Throws() => Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfDefault(0));

    [Fact]
    public void ThrowIfNotDefined_DefinedValue_DoesNotThrow() => ArgumentHelpers.ThrowIfNotDefined(DayOfWeek.Friday);

    [Fact]
    public void ThrowIfNotDefined_UndefinedCast_Throws()
    {
        var day = (DayOfWeek)99;
        var ex = Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfNotDefined(day));
        Assert.Equal("day", ex.ParamName);
        Assert.Contains("DayOfWeek", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(2, 3)]
    public void ThrowIfIndexOutOfRange_ValidIndex_DoesNotThrow(int index, int count) => ArgumentHelpers.ThrowIfIndexOutOfRange(index, count);

    [Theory]
    [InlineData(-1, 3)]
    [InlineData(3, 3)]
    [InlineData(0, 0)]
    public void ThrowIfIndexOutOfRange_InvalidIndex_Throws(int index, int count)
    {
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfIndexOutOfRange(index, count));
        Assert.Equal("index", ex.ParamName);
        Assert.Equal(index, ex.ActualValue);
        Assert.Equal(0, ex.MinValue);
        Assert.Equal(count - 1, ex.MaxValue);
    }
}
