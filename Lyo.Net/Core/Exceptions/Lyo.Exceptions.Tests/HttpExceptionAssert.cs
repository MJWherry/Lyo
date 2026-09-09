using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

internal static class HttpExceptionAssert
{
    public static void DefaultHas(HttpException ex, int status, string message)
    {
        Assert.Equal(status, ex.StatusCode);
        Assert.Equal(message, ex.Message);
    }

    public static void MessageIs(Exception ex, string message) => Assert.Equal(message, ex.Message);

    public static void InnerIs(Exception ex, Exception inner) => Assert.Same(inner, ex.InnerException);
}
