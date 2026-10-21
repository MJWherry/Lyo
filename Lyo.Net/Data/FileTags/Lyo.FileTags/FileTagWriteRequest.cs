using Lyo.Exceptions;

namespace Lyo.FileTags;

/// <summary>Tags to write onto a file. <see cref="ClearExisting" /> drops tags the provider already stored before the new values are applied.</summary>
public sealed class FileTagWriteRequest
{
    /// <summary>Tags to set. A key already on the file is replaced.</summary>
    public IReadOnlyDictionary<FileTagKey, string> Tags { get; }

    /// <summary>When true, the provider drops existing tags before applying <see cref="Tags" />.</summary>
    public bool ClearExisting { get; }

    /// <summary>Builds a write. <paramref name="tags" /> may be empty when <paramref name="clearExisting" /> is true.</summary>
    public FileTagWriteRequest(IReadOnlyDictionary<FileTagKey, string> tags, bool clearExisting = false)
    {
        ArgumentHelpers.ThrowIfNull(tags);
        Tags = tags;
        ClearExisting = clearExisting;
    }
}