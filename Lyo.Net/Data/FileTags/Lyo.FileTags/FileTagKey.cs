using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.FileTags;

/// <summary>
/// Catalog key for a file tag. Built-ins cover the tags file storage and media both read. <see cref="Custom" /> keeps any other name. Ids match the lowercase token stored in
/// <c>fileTags</c> and passed to ffmpeg <c>-metadata</c> (<c>album_artist</c>, not <c>albumartist</c>).
/// </summary>
[DebuggerDisplay("{Name} ({Id})")]
public sealed record FileTagKey
{
    private static readonly ConcurrentDictionary<string, FileTagKey> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, FileTagKey> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Title. Id <c>title</c>.</summary>
    public static readonly FileTagKey Title = Create("Title", "title");

    /// <summary>Artist or creator. Id <c>artist</c>.</summary>
    public static readonly FileTagKey Artist = Create("Artist", "artist");

    /// <summary>Album. Id <c>album</c>.</summary>
    public static readonly FileTagKey Album = Create("Album", "album");

    /// <summary>Album artist. Id <c>album_artist</c>.</summary>
    public static readonly FileTagKey AlbumArtist = Create("AlbumArtist", "album_artist");

    /// <summary>Comment. Id <c>comment</c>.</summary>
    public static readonly FileTagKey Comment = Create("Comment", "comment");

    /// <summary>Genre. Id <c>genre</c>.</summary>
    public static readonly FileTagKey Genre = Create("Genre", "genre");

    /// <summary>Date. Id <c>date</c>. Providers map capture time and EXIF date fields here.</summary>
    public static readonly FileTagKey Date = Create("Date", "date");

    /// <summary>Author. Id <c>author</c>.</summary>
    public static readonly FileTagKey Author = Create("Author", "author");

    /// <summary>Subject. Id <c>subject</c>.</summary>
    public static readonly FileTagKey Subject = Create("Subject", "subject");

    /// <summary>Description. Id <c>description</c>.</summary>
    public static readonly FileTagKey Description = Create("Description", "description");

    /// <summary>Copyright. Id <c>copyright</c>.</summary>
    public static readonly FileTagKey Copyright = Create("Copyright", "copyright");

    /// <summary>Keywords. Id <c>keywords</c>.</summary>
    public static readonly FileTagKey Keywords = Create("Keywords", "keywords");

    /// <summary>Every built-in. Touching this list registers the catalog.</summary>
    public static IReadOnlyList<FileTagKey> BuiltIns { get; } = [
        Title, Artist, Album, AlbumArtist, Comment, Genre, Date, Author, Subject, Description,
        Copyright, Keywords
    ];

    /// <summary>Display name (<c>Title</c>, <c>AlbumArtist</c>, or the custom id).</summary>
    public string Name { get; }

    /// <summary>Stable id (<c>title</c>, <c>album_artist</c>, or the normalized custom id).</summary>
    public string Id { get; }

    private FileTagKey(string name, string id)
    {
        Name = name;
        Id = id;
    }

    /// <summary>Resolves a built-in or previously registered custom id. Null when the id is empty or unknown.</summary>
    public static FileTagKey? TryFromId(string? id) => FileTagCatalog.TryFromId(ById, id);

    /// <summary>Resolves a built-in or previously registered custom name. Null when the name is empty or unknown.</summary>
    public static FileTagKey? TryFromName(string? name) => FileTagCatalog.TryFromName(ByName, name);

    /// <summary>Returns the built-in with this id, or a custom key whose <see cref="Id" /> is the normalized text. The same id always returns the same instance.</summary>
    public static FileTagKey Custom(string id)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        return FileTagCatalog.GetOrCustom(ById, ByName, id, key => new(key, key));
    }

    private static FileTagKey Create(string name, string id) => FileTagCatalog.Register(ByName, ById, new(name, id), name, id);
}