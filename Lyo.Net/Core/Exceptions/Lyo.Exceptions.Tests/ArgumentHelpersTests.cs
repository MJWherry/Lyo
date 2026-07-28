using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class ArgumentHelpersTests
{
    [Fact]
    public void ThrowIfNull_NonNull_DoesNotThrow() => ArgumentHelpers.ThrowIfNull(new());

    [Fact]
    public void ThrowIfNull_Null_ThrowsWithCallerExpressionAsParamName()
    {
        object? connection = null;
        var ex = Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfNull(connection));
        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNull_Null_WithExplicitParamName_UsesIt()
    {
        object? value = null;
        var ex = Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfNull(value, "options.BaseUrl"));
        Assert.Equal("options.BaseUrl", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNullReturn_NonNull_ReturnsSameInstance()
    {
        var instance = new object();
        Assert.Same(instance, ArgumentHelpers.ThrowIfNullReturn(instance));
    }

    [Fact]
    public void ThrowIfNullReturn_Null_Throws()
    {
        string? name = null;
        var ex = Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfNullReturn(name));
        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNullOrWhiteSpace_Value_DoesNotThrow() => ArgumentHelpers.ThrowIfNullOrWhiteSpace("value");

    [Fact]
    public void ThrowIfNullOrWhiteSpace_Null_ThrowsArgumentNull()
    {
        string? value = null;
        Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ThrowIfNullOrWhiteSpace_EmptyOrWhitespace_ThrowsArgument(string value)
    {
        var ex = Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNullOrEmpty_String_WhitespaceAllowed() => ArgumentHelpers.ThrowIfNullOrEmpty("   ");

    [Fact]
    public void ThrowIfNullOrEmpty_String_Null_ThrowsArgumentNull()
    {
        string? value = null;
        Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfNullOrEmpty(value));
    }

    [Fact]
    public void ThrowIfNullOrEmpty_String_Empty_ThrowsArgument()
    {
        var ex = Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfNullOrEmpty(""));
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThrowIfWhiteSpaceOrEmpty_Null_DoesNotThrow() => ArgumentHelpers.ThrowIfWhiteSpaceOrEmpty(null);

    [Fact]
    public void ThrowIfWhiteSpaceOrEmpty_Value_DoesNotThrow() => ArgumentHelpers.ThrowIfWhiteSpaceOrEmpty("value");

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ThrowIfWhiteSpaceOrEmpty_EmptyOrWhitespace_Throws(string value) => Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfWhiteSpaceOrEmpty(value));

    [Fact]
    public void ThrowIf_False_DoesNotThrow() => ArgumentHelpers.ThrowIf(false, "unused");

    [Fact]
    public void ThrowIf_True_ThrowsWithMessageAndParamName()
    {
        var ex = Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIf(true, "Bad argument.", "arg"));
        Assert.StartsWith("Bad argument.", ex.Message, StringComparison.Ordinal);
        Assert.Equal("arg", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNotInRange_InRange_DoesNotThrow() => ArgumentHelpers.ThrowIfNotInRange(5, 1, 10);

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void ThrowIfNotInRange_OutOfRange_Throws(int candidate)
    {
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNotInRange(candidate, 1, 10));
        Assert.Equal("candidate", ex.ParamName);
        Assert.Equal(candidate, ex.ActualValue);
        Assert.Equal(1, ex.MinValue);
        Assert.Equal(10, ex.MaxValue);
    }

    [Fact]
    public void ThrowIfNotInRange_DateTime_InRange_DoesNotThrow() => ArgumentHelpers.ThrowIfNotInRange(new(2024, 6, 1), new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

    [Fact]
    public void ThrowIfNotInRange_DateTime_OutOfRange_Throws()
    {
        var when = new DateTime(2025, 1, 1);
        Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNotInRange(when, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
    }

    [Fact]
    public void ThrowIfNullOrNotInRange_DateTime_Null_Throws()
    {
        DateTime? when = null;
        Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfNullOrNotInRange(when, new DateTime(2024, 1, 1)));
    }

    [Fact]
    public void ThrowIfNotInRange_TimeSpan_OutOfRange_Throws()
    {
        var duration = TimeSpan.FromMinutes(90);
        Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNotInRange(duration, TimeSpan.Zero, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void ThrowIfNullOrNotInRange_TimeSpan_Null_Throws()
    {
        TimeSpan? duration = null;
        Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfNullOrNotInRange(duration, TimeSpan.Zero, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void ThrowIfNullOrNotInRange_Array_Null_Throws()
    {
        int[]? values = null;
        Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfNullOrNotInRange(values, 1, 5));
    }

    [Fact]
    public void ThrowIfNotInRange_Array_LengthOutOfRange_Throws()
    {
        var values = new int[6];
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNotInRange(values, 1, 5));
        Assert.Contains("Array length (6)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfNotInRange_Array_LengthInRange_DoesNotThrow() => ArgumentHelpers.ThrowIfNotInRange(new int[3], 1, 5);

    [Fact]
    public void ThrowIfNullOrEmpty_Collection_NonEmpty_DoesNotThrow() => ArgumentHelpers.ThrowIfNullOrEmpty(new[] { 1 });

    [Fact]
    public void ThrowIfNullOrEmpty_Collection_Null_ThrowsArgumentNull()
    {
        List<int>? items = null;
        Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfNullOrEmpty(items));
    }

    [Fact]
    public void ThrowIfNullOrEmpty_Collection_Empty_ThrowsArgument() => Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfNullOrEmpty(new List<int>()));

    [Fact]
    public void ThrowIfNullOrEmpty_LazyEnumerable_Empty_ThrowsArgument()
        => Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfNullOrEmpty(Enumerable.Range(0, 5).Where(i => i > 10)));

    [Fact]
    public void ThrowIfNullOrEmpty_Dictionary_Empty_ThrowsArgument() => Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfNullOrEmpty(new Dictionary<string, int>()));

    [Fact]
    public void ThrowIfNullOrEmpty_Dictionary_NonEmpty_DoesNotThrow() => ArgumentHelpers.ThrowIfNullOrEmpty(new Dictionary<string, int> { ["a"] = 1 });

    [Fact]
    public void ThrowIfZero_NonZero_DoesNotThrow() => ArgumentHelpers.ThrowIfZero(7);

    [Fact]
    public void ThrowIfZero_Zero_Throws()
    {
        var count = 0;
        var ex = Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfZero(count));
        Assert.Equal("count", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void ThrowIfNegative_NonNegative_DoesNotThrow(int value) => ArgumentHelpers.ThrowIfNegative(value);

    [Fact]
    public void ThrowIfNegative_Negative_Throws() => Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNegative(-1));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ThrowIfNegativeOrZero_NonPositive_Throws(int value) => Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNegativeOrZero(value));

    [Fact]
    public void ThrowIfNegativeOrZero_Positive_DoesNotThrow() => ArgumentHelpers.ThrowIfNegativeOrZero(1);

    [Fact]
    public void ThrowIfPositive_Positive_Throws() => Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfPositive(1));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ThrowIfPositive_NonPositive_DoesNotThrow(int value) => ArgumentHelpers.ThrowIfPositive(value);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ThrowIfPositiveOrZero_NonNegative_Throws(int value) => Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfPositiveOrZero(value));

    [Fact]
    public void ThrowIfPositiveOrZero_Negative_DoesNotThrow() => ArgumentHelpers.ThrowIfPositiveOrZero(-1);

    [Fact]
    public void ThrowIfGreaterThan_Above_Throws() => Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfGreaterThan(11, 10));

    [Fact]
    public void ThrowIfGreaterThan_Equal_DoesNotThrow() => ArgumentHelpers.ThrowIfGreaterThan(10, 10);

    [Fact]
    public void ThrowIfGreaterThanOrEqual_Equal_Throws() => Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfGreaterThanOrEqual(10, 10));

    [Fact]
    public void ThrowIfGreaterThanOrEqual_Below_DoesNotThrow() => ArgumentHelpers.ThrowIfGreaterThanOrEqual(9, 10);

    [Fact]
    public void ThrowIfLessThan_Below_Throws() => Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfLessThan(9, 10));

    [Fact]
    public void ThrowIfLessThan_Equal_DoesNotThrow() => ArgumentHelpers.ThrowIfLessThan(10, 10);

    [Fact]
    public void ThrowIfLessThanOrEqual_Equal_Throws() => Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfLessThanOrEqual(10, 10));

    [Fact]
    public void ThrowIfLessThanOrEqual_Above_DoesNotThrow() => ArgumentHelpers.ThrowIfLessThanOrEqual(11, 10);

    [Fact]
    public void ThrowIfNotNullAndLessThanOrEqual_Null_DoesNotThrow() => ArgumentHelpers.ThrowIfNotNullAndLessThanOrEqual(null, 5.0);

    [Fact]
    public void ThrowIfNotNullAndLessThanOrEqual_Above_DoesNotThrow() => ArgumentHelpers.ThrowIfNotNullAndLessThanOrEqual(6.0, 5.0);

    [Fact]
    public void ThrowIfNotNullAndLessThanOrEqual_Equal_Throws() => Assert.Throws<ArgumentOutsideRangeException>(() => ArgumentHelpers.ThrowIfNotNullAndLessThanOrEqual(5.0, 5.0));

    [Fact]
    public void ThrowIfEqual_Equal_Throws() => Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfEqual(5, 5));

    [Fact]
    public void ThrowIfEqual_NotEqual_DoesNotThrow() => ArgumentHelpers.ThrowIfEqual(5, 6);

    [Fact]
    public void ThrowIfNotEqual_NotEqual_Throws() => Assert.Throws<ArgumentException>(() => ArgumentHelpers.ThrowIfNotEqual(5, 6));

    [Fact]
    public void ThrowIfNotEqual_Equal_DoesNotThrow() => ArgumentHelpers.ThrowIfNotEqual(5, 5);

    [Fact]
    public void ThrowIfFileNotFound_Path_Existing_DoesNotThrow()
    {
        var path = Path.GetTempFileName();
        try {
            ArgumentHelpers.ThrowIfFileNotFound(path);
        }
        finally {
            File.Delete(path);
        }
    }

    [Fact]
    public void ThrowIfFileNotFound_Path_Missing_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.txt");
        var ex = Assert.Throws<FileNotFoundException>(() => ArgumentHelpers.ThrowIfFileNotFound(path));
        Assert.Equal(path, ex.FileName);
    }

    [Fact]
    public void ThrowIfFileNotFound_FileInfo_Null_Throws()
    {
        FileInfo? fileInfo = null;
        Assert.Throws<ArgumentNullException>(() => ArgumentHelpers.ThrowIfFileNotFound(fileInfo));
    }

    [Fact]
    public void ThrowIfFileNotFound_FileInfo_Missing_Throws()
    {
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.txt"));
        Assert.Throws<FileNotFoundException>(() => ArgumentHelpers.ThrowIfFileNotFound(fileInfo));
    }
}