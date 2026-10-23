using Lyo.Result;

namespace Lyo.FileTags;

/// <summary>Reads tags from one family of files. <see cref="CanRead" /> is the gate. A matching file that cannot be parsed returns a failed result.</summary>
public interface IFileTagReader
{
    /// <summary>True when this reader understands <paramref name="path" /> (usually by extension).</summary>
    bool CanRead(string path);

    /// <summary>Reads tags from <paramref name="path" />. Call only after <see cref="CanRead" /> is true.</summary>
    Task<Result<FileTags>> ReadAsync(string path, CancellationToken ct = default);
}