using System.Globalization;
using System.Text.Json;
using Lyo.Common.Metadata.Records;
using Lyo.FileMetadataStore.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStoreMetadataTable
{
    /// <summary>Metadata row to show. Ignored when null.</summary>
    [Parameter]
    public FileStoreResult? Metadata { get; set; }

    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    /// <summary>Label/value pairs shared by the table and the narrow-viewport card so both stay aligned.</summary>
    private IReadOnlyList<(string Label, string Value)> Rows
        => Metadata is not { } m
            ? []
            : [
                ("Id", m.Id.ToString()),
                ("Status", m.DeletedAt.HasValue ? $"Deleted ({m.DeletedAt.Value.ToUniversalTime():u})" : "Active"),
                ("Original name", m.OriginalFileName ?? "-"),
                ("Original size", FormatBytes(m.OriginalFileSize)),
                ("Stored source", m.SourceFileName),
                ("Stored size", FormatBytes(m.SourceFileSize)),
                ("Compressed", m.IsCompressed ? m.CompressionAlgorithm?.ToString() ?? "Yes" : "No"),
                ("Encrypted", m.IsEncrypted ? $"{m.DataEncryptionKeyId} / {m.DataEncryptionKeyVersion}" : "No"),
                ("Path prefix", m.PathPrefix ?? "-"),
                ("Timestamp", m.Timestamp.ToString("u", CultureInfo.InvariantCulture)),
                ("Hash algorithm", m.HashAlgorithm?.ToString() ?? "-"),
                ("Original hash", FormatHash(m.OriginalFileHash)),
                ("Stored hash", FormatHash(m.SourceFileHash)),
                ("Compressed size", FormatBytes(m.CompressedFileSize)),
                ("Compressed hash", FormatHash(m.CompressedFileHash)),
                ("Data key algorithm", m.DataEncryptionKeyAlgorithm?.ToString() ?? "-"),
                ("Key encryption algorithm", m.KeyEncryptionKeyAlgorithm?.ToString() ?? "-"),
                ("Encrypted size", FormatBytes(m.EncryptedFileSize)),
                ("Encrypted hash", FormatHash(m.EncryptedFileHash)),
                ("KEK salt", FormatHash(m.KeyEncryptionKeySalt)),
                ("Metadata", FormatMetadata(m.Metadata))
            ];

    private static string FormatBytes(long size) => FileSizeUnitInfo.FormatBestFit(size);

    private static string FormatBytes(long? size) => size.HasValue ? FormatBytes(size.Value) : "-";

    private static string FormatHash(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return "-";

        return Convert.ToHexString(bytes);
    }

    private static string FormatMetadata(JsonElement? metadata)
    {
        if (metadata is not { } el || el.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return "-";

        return JsonSerializer.Serialize(el, PrettyJson);
    }
}
