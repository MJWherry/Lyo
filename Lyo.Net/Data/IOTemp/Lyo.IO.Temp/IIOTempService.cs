using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp;

// ReSharper disable once InconsistentNaming
public interface IIOTempService : IDisposable
{
    int ActiveSessionCount { get; }

    // Session-based (primary usage)
    IIOTempSession CreateSession(IOTempSessionOptions? options = null);

    // Quick one-offs without a session
    string CreateFile(string? name = null);

    string CreateFile(ReadOnlyMemory<byte> data, string? name = null);

    string CreateFile(Stream data, string? name = null);

    string CreateDirectory(string? name = null);

    // Cleanup
    void Cleanup();

    Task CleanupAsync(CancellationToken ct = default);

    Task CleanupAsync(TimeSpan olderThan, CancellationToken ct = default);
}