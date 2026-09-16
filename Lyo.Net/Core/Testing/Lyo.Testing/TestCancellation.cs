using Xunit;

namespace Lyo.Testing;

/// <summary>Current xUnit cancellation token for tests that have no fixture. Reads <see cref="TestContext.Current"/> when accessed.</summary>
public static class TestCancellation
{
    /// <summary>The cancellation token for the test that is running. `*.Tests` projects import this class statically, so call sites pass <c>TestCt</c>.</summary>
    public static CancellationToken TestCt => TestContext.Current.CancellationToken;
}
