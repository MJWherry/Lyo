using Lyo.Result;

namespace Lyo.FileTags;

/// <summary>Writes tags onto one family of files. <see cref="CanWrite" /> is the gate.</summary>
public interface IFileTagWriter
{
    /// <summary>True when this writer understands <paramref name="path" /> (usually by extension).</summary>
    bool CanWrite(string path);

    /// <summary>Writes <paramref name="request" /> onto <paramref name="path" />. Call only after <see cref="CanWrite" /> is true.</summary>
    Task<Result<FileTags>> WriteAsync(string path, FileTagWriteRequest request, CancellationToken ct = default);
}
