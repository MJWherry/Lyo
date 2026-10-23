using System.Diagnostics;

namespace Lyo.FileMetadataStore.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class FileDataEntity
{
    public Guid FileId { get; set; }

    public byte[] Data { get; set; } = null!;

    /// <summary>Type name and file id.</summary>
    public override string ToString() => $"{GetType().Name} {FileId}";
}