using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class RangeGuardRegressionTests
{
    [Fact]
    public void ArgumentThrowIfNotInRange_NoBounds_DoesNotThrow()
    {
        // Regression: unconstrained T? defaults collapsed to 0 for value types, so a no-bounds call enforced the range [0, 0].
        ArgumentHelpers.ThrowIfNotInRange(-5);
        ArgumentHelpers.ThrowIfNotInRange(int.MaxValue);
    }

    [Fact]
    public void ArgumentThrowIfNotInRange_MinOnly_EnforcesOnlyMin()
    {
        ArgumentHelpers.ThrowIfNotInRange(int.MaxValue, 0);
        Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNotInRange(-1, 0));
    }

    [Fact]
    public void ArgumentThrowIfNotInRange_MaxOnly_EnforcesOnlyMax()
    {
        ArgumentHelpers.ThrowIfNotInRange(int.MinValue, max: 10);
        Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNotInRange(11, max: 10));
    }

    [Fact]
    public void ArgumentThrowIfNullOrNotInRange_NoBounds_NonNull_DoesNotThrow()
    {
        int? value = -5;
        ArgumentHelpers.ThrowIfNullOrNotInRange(value);
    }

    [Fact]
    public void ArgumentThrowIfNotInRange_String_UsesClassOverload()
    {
        ArgumentHelpers.ThrowIfNotInRange("m", "a", "z");
        Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNotInRange("0", "a", "z"));
    }

    [Fact]
    public void OperationThrowIfNotInRange_NoBounds_DoesNotThrow()
    {
        OperationHelpers.ThrowIfNotInRange(-5);
        OperationHelpers.ThrowIfNotInRange(int.MaxValue);
    }

    [Fact]
    public void OperationThrowIfNullOrNotInRange_NullableStruct_Null_Throws()
    {
        int? value = null;
        Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNullOrNotInRange(value, 1, 10));
    }

    [Fact]
    public void ThrowIfNegative_ReportsZeroAsMinimum()
    {
        // Regression: the reported minimum bound was 1 although 0 is accepted.
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNegative(-1));
        Assert.Equal(0, ex.MinValue);
    }
}

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