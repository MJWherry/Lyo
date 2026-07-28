namespace Lyo.Exceptions.Tests;

public class OperationHelpersTests
{
    [Fact]
    public void ThrowIf_False_DoesNotThrow() => OperationHelpers.ThrowIf(false, "unused");

    [Fact]
    public void ThrowIf_True_ThrowsWithMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIf(true, "State is invalid."));
        Assert.Equal("State is invalid.", ex.Message);
    }

    [Fact]
    public void ThrowIfNull_NonNull_DoesNotThrow() => OperationHelpers.ThrowIfNull(new());

    [Fact]
    public void ThrowIfNull_Null_ThrowsWithDefaultMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNull(null));
        Assert.Contains("required value is null", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfNull_Null_WithCustomMessage_UsesMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNull(null, "Connection not initialized."));
        Assert.Equal("Connection not initialized.", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ThrowIfNullOrWhiteSpace_Missing_Throws(string? value) => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNullOrWhiteSpace(value));

    [Fact]
    public void ThrowIfNullOrWhiteSpace_Value_DoesNotThrow() => OperationHelpers.ThrowIfNullOrWhiteSpace("value");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ThrowIfNullOrEmpty_String_Missing_Throws(string? value) => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNullOrEmpty(value));

    [Fact]
    public void ThrowIfNullOrEmpty_String_Whitespace_DoesNotThrow() => OperationHelpers.ThrowIfNullOrEmpty("  ");

    [Fact]
    public void ThrowIfNullOrEmpty_Collection_Null_Throws()
    {
        List<int>? items = null;
        Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNullOrEmpty(items));
    }

    [Fact]
    public void ThrowIfNullOrEmpty_Collection_Empty_ThrowsWithParamNameInMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNullOrEmpty(new List<int>(), "items"));
        Assert.Equal("Collection 'items' cannot be empty.", ex.Message);
    }

    [Fact]
    public void ThrowIfNullOrEmpty_Collection_NonEmpty_DoesNotThrow() => OperationHelpers.ThrowIfNullOrEmpty(new[] { 1 });

    [Fact]
    public void ThrowIfNotReadable_Readable_DoesNotThrow()
    {
        using var stream = new MemoryStream();
        OperationHelpers.ThrowIfNotReadable(stream);
    }

    [Fact]
    public void ThrowIfNotReadable_Null_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNotReadable(null));

    [Fact]
    public void ThrowIfNotReadable_Disposed_Throws()
    {
        var stream = new MemoryStream();
        stream.Dispose();
        Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNotReadable(stream));
    }

    [Fact]
    public void ThrowIfNotWritable_Writable_DoesNotThrow()
    {
        using var stream = new MemoryStream();
        OperationHelpers.ThrowIfNotWritable(stream);
    }

    [Fact]
    public void ThrowIfNotWritable_ReadOnly_Throws()
    {
        using var stream = new MemoryStream(new byte[] { 1 }, false);
        Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNotWritable(stream));
    }

    [Fact]
    public void ThrowIfDisposed_False_DoesNotThrow() => OperationHelpers.ThrowIfDisposed(false);

    [Fact]
    public void ThrowIfDisposed_True_ThrowsWithObjectName()
    {
        var ex = Assert.Throws<ObjectDisposedException>(() => OperationHelpers.ThrowIfDisposed(true, "MyResource"));
        Assert.Equal("MyResource", ex.ObjectName);
    }

    [Fact]
    public void ThrowIfCancelled_NotCancelled_DoesNotThrow() => OperationHelpers.ThrowIfCancelled(CancellationToken.None);

    [Fact]
    public void ThrowIfCancelled_Cancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ex = Assert.Throws<OperationCanceledException>(() => OperationHelpers.ThrowIfCancelled(cts.Token));
        Assert.Equal(cts.Token, ex.CancellationToken);
    }

    [Fact]
    public void ThrowIfNotSupported_False_DoesNotThrow() => OperationHelpers.ThrowIfNotSupported(false);

    [Fact]
    public void ThrowIfNotSupported_True_Throws()
    {
        var ex = Assert.Throws<NotSupportedException>(() => OperationHelpers.ThrowIfNotSupported(true, "Csv export is not supported."));
        Assert.Equal("Csv export is not supported.", ex.Message);
    }

    [Fact]
    public void ThrowIfNotInRange_InRange_DoesNotThrow() => OperationHelpers.ThrowIfNotInRange(5, 1, 10);

    [Fact]
    public void ThrowIfNotInRange_OutOfRange_ThrowsInvalidOperationWithParamHint()
    {
        var retryCount = 11;
        var ex = Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNotInRange(retryCount, 1, 10));
        Assert.Contains("not in the allowed range [1, 10]", ex.Message, StringComparison.Ordinal);
        Assert.Contains("(retryCount)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfNotInRange_DateTime_OutOfRange_Throws()
    {
        var when = new DateTime(2025, 1, 1);
        Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNotInRange(when, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
    }

    [Fact]
    public void ThrowIfNullOrNotInRange_DateTime_Null_Throws()
    {
        DateTime? when = null;
        Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNullOrNotInRange(when, new DateTime(2024, 1, 1)));
    }

    [Fact]
    public void ThrowIfNotInRange_TimeSpan_OutOfRange_Throws()
        => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNotInRange(TimeSpan.FromHours(2), TimeSpan.Zero, TimeSpan.FromHours(1)));

    [Fact]
    public void ThrowIfNullOrNotInRange_TimeSpan_Null_Throws()
    {
        TimeSpan? duration = null;
        Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNullOrNotInRange(duration, TimeSpan.Zero));
    }

    [Fact]
    public void ThrowIfNullOrNotInRange_Array_Null_Throws()
    {
        int[]? values = null;
        Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNullOrNotInRange(values, 1, 5));
    }

    [Fact]
    public void ThrowIfNotInRange_Array_LengthOutOfRange_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNotInRange(new int[6], 1, 5));

    [Fact]
    public void ThrowIfZero_Zero_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfZero(0));

    [Fact]
    public void ThrowIfZero_NonZero_DoesNotThrow() => OperationHelpers.ThrowIfZero(3);

    [Fact]
    public void ThrowIfNegative_Negative_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNegative(-1));

    [Fact]
    public void ThrowIfNegative_Zero_DoesNotThrow() => OperationHelpers.ThrowIfNegative(0);

    [Fact]
    public void ThrowIfNegativeOrZero_Zero_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNegativeOrZero(0));

    [Fact]
    public void ThrowIfPositive_Positive_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfPositive(1));

    [Fact]
    public void ThrowIfPositiveOrZero_Zero_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfPositiveOrZero(0));

    [Fact]
    public void ThrowIfGreaterThan_Above_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfGreaterThan(11, 10));

    [Fact]
    public void ThrowIfGreaterThanOrEqual_Equal_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfGreaterThanOrEqual(10, 10));

    [Fact]
    public void ThrowIfLessThan_Below_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfLessThan(9, 10));

    [Fact]
    public void ThrowIfLessThanOrEqual_Equal_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfLessThanOrEqual(10, 10));

    [Fact]
    public void ThrowIfNotNullAndLessThanOrEqual_Null_DoesNotThrow() => OperationHelpers.ThrowIfNotNullAndLessThanOrEqual(null, 5.0);

    [Fact]
    public void ThrowIfNotNullAndLessThanOrEqual_Equal_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNotNullAndLessThanOrEqual(5.0, 5.0));

    [Fact]
    public void ThrowIfEqual_Equal_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfEqual(5, 5));

    [Fact]
    public void ThrowIfNotEqual_NotEqual_Throws() => Assert.Throws<InvalidOperationException>(() => OperationHelpers.ThrowIfNotEqual(5, 6));

    [Fact]
    public void ThrowIfNotEqual_Equal_DoesNotThrow() => OperationHelpers.ThrowIfNotEqual(5, 5);
}