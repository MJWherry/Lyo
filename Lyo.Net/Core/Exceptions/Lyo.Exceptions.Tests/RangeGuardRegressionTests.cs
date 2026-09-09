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
        // Regression: the reported minimum was 1 although 0 is accepted.
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNegative(-1));
        Assert.Equal(0, ex.MinValue);
    }
}
