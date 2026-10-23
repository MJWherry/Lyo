using System.Diagnostics;

namespace Lyo.FileMetadataStore.Models;

/// <summary>Persisted file tag. <see cref="FileId" /> matches <see cref="FileMetadataEntity.Id" /> (Guid text).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FileTagEntity
{
    public Guid Id { get; set; }

    public string FileId { get; set; } = "";

    public string TagType { get; set; } = "";

    public string Value { get; set; } = "";

    public string Slug { get; set; } = "";

    public bool Automatic { get; set; }

    public FileTag ToFileTag() => new(Id, Guid.Parse(FileId), TagType, Value, Slug, Automatic);

    /// <summary>Type name, id, and slug.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Slug}";
}