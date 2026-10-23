using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.FileMetadataStore.Models;

/// <summary>One level of a tag grouping. The kind says how to parse the stored text. The grain says which part is the folder name.</summary>
/// <param name="TagType">Tag dimension.</param>
/// <param name="Kind">How the stored text is parsed.</param>
/// <param name="Grain">Which part of the parsed value is the folder name.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FileTagGroupKey(string TagType, FileTagValueKind Kind, FileTagGrain Grain)
{
    /// <summary>Builds a key whose kind is the one <paramref name="grain" /> implies when the query omits a kind.</summary>
    /// <param name="tagType">Tag dimension.</param>
    /// <param name="grain">Folder component. <see cref="FileTagGrain.Value" /> is a string.</param>
    public FileTagGroupKey(string tagType, FileTagGrain grain)
        : this(tagType, FileTagValueKinds.KindForGrain(grain), grain) { }

    /// <summary>
    /// Parses <c>client</c>, <c>imported:year</c>, <c>amount:decimal</c>, or <c>sla:timespan:hours</c>. A known grain with no kind is a date-time for year, month, and day, a
    /// time of day for hour, minute, and second, and a duration for days, hours, minutes, and seconds.
    /// </summary>
    /// <param name="text">Query token.</param>
    /// <returns>A group key.</returns>
    public static FileTagGroupKey Parse(string text)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        var parts = text.Split(':');
        if (parts.Length > 3)
            throw new ArgumentException($"Group key '{text}' has too many parts.", nameof(text));

        var type = parts[0].Trim();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(type);
        if (parts.Length == 1)
            return new(type, FileTagValueKind.String, FileTagGrain.Value);

        if (parts.Length == 2)
            return ParseTwo(type, parts[1].Trim(), text);

        if (!FileTagValueKinds.TryParse(parts[1].Trim(), out var kind) || !TryParseGrain(parts[2].Trim(), out var grain))
            throw new ArgumentException($"Group key '{text}' is not a tag type, kind, and grain.", nameof(text));

        var key = new FileTagGroupKey(type, kind, grain);
        ThrowIfIllegal(key, text);
        return key;
    }

    /// <summary>Query token <see cref="Parse" /> accepts. A string value is the type alone. A grain that implies this kind is <c>type:grain</c>.</summary>
    /// <returns>The token for this key.</returns>
    public string Format()
    {
        if (Kind == FileTagValueKind.String && Grain == FileTagGrain.Value)
            return TagType;

        var grain = Grain.ToString().ToLowerInvariant();
        if (Grain != FileTagGrain.Value && FileTagValueKinds.KindForGrain(Grain) == Kind)
            return TagType + ":" + grain;

        var kind = FileTagValueKinds.Name(Kind);
        if (Grain == FileTagGrain.Value)
            return TagType + ":" + kind;

        return TagType + ":" + kind + ":" + grain;
    }

    private static FileTagGroupKey ParseTwo(string type, string token, string text)
    {
        if (TryParseGrain(token, out var grain)) {
            var keyed = new FileTagGroupKey(type, FileTagValueKinds.KindForGrain(grain), grain);
            ThrowIfIllegal(keyed, text);
            return keyed;
        }

        if (!FileTagValueKinds.TryParse(token, out var kind))
            throw new ArgumentException($"Group key '{text}' is not a tag type, kind, or grain.", nameof(text));

        var key = new FileTagGroupKey(type, kind, FileTagGrain.Value);
        ThrowIfIllegal(key, text);
        return key;
    }

    private static bool TryParseGrain(string text, out FileTagGrain grain) => Enum.TryParse(text, true, out grain);

    private static void ThrowIfIllegal(FileTagGroupKey key, string text)
    {
        var imported = string.Equals(key.TagType, FileTagTypes.Imported, StringComparison.Ordinal);
        if (imported && key.Grain == FileTagGrain.Value)
            throw new ArgumentException($"Group key '{text}' cannot group imported by its raw timestamp.", nameof(text));

        if (!FileTagValueKinds.Allows(key.Kind, key.Grain))
            throw new ArgumentException($"Group key '{text}' uses grain {key.Grain}, which {FileTagValueKinds.Name(key.Kind)} does not allow.", nameof(text));
    }
}