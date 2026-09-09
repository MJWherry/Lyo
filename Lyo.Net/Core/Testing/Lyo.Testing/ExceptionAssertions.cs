using Xunit;

namespace Lyo.Testing;

public static class ExceptionAssertions
{
    public static T Throws<T>(Action action, string? message = null)
        where T : Exception
    {
        var exception = Assert.Throws<T>(action);
        if (!string.IsNullOrWhiteSpace(message))
            Assert.Contains(message, exception.Message);

        return exception;
    }

    public static async Task<T> ThrowsAsync<T>(Func<Task> action, string? message = null)
        where T : Exception
    {
        var exception = await Assert.ThrowsAsync<T>(action);
        if (!string.IsNullOrWhiteSpace(message))
            Assert.Contains(message, exception.Message);

        return exception;
    }

    public static void ThrowsAny(Action action, params Type[] exceptionTypes)
    {
        var exception = Record.Exception(action);
        Assert.NotNull(exception);
        Assert.True(exceptionTypes.Contains(exception.GetType()), $"Expected one of {string.Join(", ", exceptionTypes.Select(t => t.Name))}, but got {exception.GetType().Name}");
    }

    public static async Task ThrowsAnyAsync(Func<Task> action, params Type[] exceptionTypes)
    {
        var exception = await Record.ExceptionAsync(action);
        Assert.NotNull(exception);
        Assert.True(exceptionTypes.Contains(exception.GetType()), $"Expected one of {string.Join(", ", exceptionTypes.Select(t => t.Name))}, but got {exception.GetType().Name}");
    }

    public static void ThrowsWithInnerException<T>(Action action, Type innerExceptionType)
        where T : Exception
    {
        var exception = Assert.Throws<T>(action);
        Assert.NotNull(exception.InnerException);
        Assert.IsType(innerExceptionType, exception.InnerException);
    }

    public static async Task ThrowsWithInnerExceptionAsync<T>(Func<Task> action, Type innerExceptionType)
        where T : Exception
    {
        var exception = await Assert.ThrowsAsync<T>(action);
        Assert.NotNull(exception.InnerException);
        Assert.IsType(innerExceptionType, exception.InnerException);
    }

    public static void DoesNotThrow(Action action)
    {
        var exception = Record.Exception(action);
        Assert.Null(exception);
    }

    public static async Task DoesNotThrowAsync(Func<Task> action)
    {
        var exception = await Record.ExceptionAsync(action);
        Assert.Null(exception);
    }
}