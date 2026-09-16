using Lyo.Exceptions;

namespace Lyo.FileTags;

/// <summary>Tags read from a file. Keys are <see cref="FileTagKey" /> values. An empty dictionary is a successful read that found nothing.</summary>
public sealed class FileTags
{
    /// <summary>Builds a tag bag. <paramref name="tags" /> is stored as given.</summary>
    public FileTags(IReadOnlyDictionary<FileTagKey, string> tags)
    {
        ArgumentHelpers.ThrowIfNull(tags);
        Tags = tags;
    }

    /// <summary>Tags that were read or written. Missing keys were not present.</summary>
    public IReadOnlyDictionary<FileTagKey, string> Tags { get; }
}
