using Lyo.Desktop.Components;

namespace Lyo.FileManager.App;

/// <summary>One row in the file grid or the query grid.</summary>
public sealed class FileRow
{
    public string Name { get; init; } = "";

    public string SizeLabel { get; init; } = "";

    public FileGlyph Glyph { get; init; }

    public string TypeLabel { get; init; } = "";

    public int TypeHue { get; init; } = -1;

    public string CompressionLabel { get; init; } = "";

    public int CompressionHue { get; init; } = -1;

    public bool ShowCompression { get; init; }

    public string EncryptionLabel { get; init; } = "";

    public int EncryptionHue { get; init; } = -1;

    public bool ShowEncryption { get; init; }

    public string Prefix { get; init; } = "";

    public FileManagerEntry? Entry { get; init; }

    public FileManagerQueryRow? Query { get; init; }

    public static FileRow FromEntry(FileManagerEntry entry)
    {
        var type = FileKind.TypeChip(null, entry.Name, entry.IsDirectory);
        var compression = FileKind.CompressionChip(entry.IsDirectory, entry.IsCompressed, entry.Compression);
        var encryption = FileKind.EncryptionChip(entry.IsDirectory, entry.IsEncrypted, entry.Encryption);
        return new() {
            Name = entry.Pending ? entry.Name + " (pending)" : entry.Name,
            SizeLabel = entry.IsDirectory ? "" : Format(entry.Size),
            Glyph = FileKind.Glyph(entry.Name, entry.IsDirectory),
            TypeLabel = type.Label,
            TypeHue = type.Hue,
            CompressionLabel = compression?.Label ?? "",
            CompressionHue = compression?.Hue ?? -1,
            ShowCompression = compression != null,
            EncryptionLabel = encryption?.Label ?? "",
            EncryptionHue = encryption?.Hue ?? -1,
            ShowEncryption = encryption != null,
            Prefix = entry.PathPrefix ?? "",
            Entry = entry
        };
    }

    public static FileRow FromQuery(FileManagerQueryRow row)
    {
        var name = row.OriginalFileName ?? row.Id.ToString();
        var type = FileKind.TypeChip(null, name, false);
        return new() {
            Name = name,
            SizeLabel = Format(row.OriginalFileSize),
            Glyph = FileKind.Glyph(name, false),
            TypeLabel = type.Label,
            TypeHue = type.Hue,
            ShowCompression = false,
            ShowEncryption = false,
            Prefix = row.PathPrefix ?? "",
            Query = row,
            Entry = new(name, false, row.PathPrefix, row.Id, Lyo.Api.FileStorage.Models.FileStoragePresence.Store, row.OriginalFileSize, null)
        };
    }

    private static string Format(long size) => size < 1024 ? size + " B" : (size / 1024d).ToString("0.#") + " KB";
}
