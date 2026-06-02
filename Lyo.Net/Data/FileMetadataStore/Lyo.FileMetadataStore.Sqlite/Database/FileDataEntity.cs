namespace Lyo.FileMetadataStore.Sqlite.Database;

public sealed class FileDataEntity
{
    public Guid FileId { get; set; }

    public byte[] Data { get; set; } = null!;
}
