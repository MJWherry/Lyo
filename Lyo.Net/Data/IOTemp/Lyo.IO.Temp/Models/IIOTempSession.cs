namespace Lyo.IO.Temp.Models;

// ReSharper disable once InconsistentNaming
public interface IIOTempSession : IAsyncDisposable, IDisposable
{
    string SessionDirectory { get; }

    IReadOnlyList<string> Files { get; }

    IReadOnlyList<string> Directories { get; }

    // Files
    string GetFilePath(string? name = null);

    string TouchFile(string? name = null);

    string CreateFile(string text);

    string CreateFile(ReadOnlyMemory<byte> data, string? name = null);

    string CreateFile(Stream data, string? name = null);

    // Async files
    Task<string> CreateFileAsync(string text, CancellationToken ct = default);

    Task<string> CreateFileAsync(ReadOnlyMemory<byte> data, string? name = null, CancellationToken ct = default);

    Task<string> CreateFileAsync(Stream data, string? name = null, CancellationToken ct = default);

    // Directories
    string GetDirectoryPath(string? name = null);

    string CreateDirectory(string? name = null);

    Task<string> CreateDirectoryAsync(string? name = null, CancellationToken ct = default);
}