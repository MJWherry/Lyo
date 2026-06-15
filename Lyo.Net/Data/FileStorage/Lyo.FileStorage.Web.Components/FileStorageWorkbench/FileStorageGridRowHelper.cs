using System.Globalization;
using System.Text.Json;
using Lyo.Web.Components.DataGrid;

namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

/// <summary>Helpers for reading file ids from projected query rows (dynamic JSON / Guid).</summary>
public static class FileStorageGridRowHelper
{
    /// <summary>Returns whether the projected row represents a soft-deleted metadata tombstone.</summary>
    public static bool IsRowDeleted(object? row) => !string.IsNullOrWhiteSpace(GetDeletedAtDisplay(row));

    /// <summary>UTC tombstone timestamp display, or empty when the file is active.</summary>
    public static string GetDeletedAtDisplay(object? row)
    {
        var raw = ProjectedValueHelper.GetValue(row, "DeletedAt");
        if (raw == null)
            return string.Empty;

        if (raw is JsonElement je) {
            if (je.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return string.Empty;

            if (je.ValueKind == JsonValueKind.String)
                return FormatDeletedAtDisplay(je.GetString());
        }

        return raw switch {
            DateTime dt => FormatDeletedAtDisplay(dt),
            DateTimeOffset dto => FormatDeletedAtDisplay(dto.UtcDateTime),
            string s => FormatDeletedAtDisplay(s),
            var _ => ProjectedValueHelper.GetDisplayValue(row, "DeletedAt")
        };
    }

    private static string FormatDeletedAtDisplay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? FormatDeletedAtDisplay(parsed)
            : value.Trim();
    }

    private static string FormatDeletedAtDisplay(DateTime deletedAtUtc) => deletedAtUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture);

    public static string FormatDeletedAtCell(object? row, object? _) => GetDeletedAtDisplay(row) is { Length: > 0 } display ? display : "-";

    public static bool TryGetFileIdFromRow(object? row, out Guid fileId)
    {
        fileId = default;
        if (row == null)
            return false;

        var idVal = ProjectedValueHelper.GetValue(row, "Id");
        if (idVal == null)
            return false;

        if (idVal is Guid g) {
            fileId = g;
            return true;
        }

        if (idVal is string s && Guid.TryParse(s, out var parsed)) {
            fileId = parsed;
            return true;
        }

        if (idVal is JsonElement je) {
            if (je.ValueKind == JsonValueKind.String && Guid.TryParse(je.GetString(), out parsed)) {
                fileId = parsed;
                return true;
            }
        }

        return false;
    }

    public static object[] GetFileRowKey(object? item) => TryGetFileIdFromRow(item, out var id) ? [id.ToString()] : [];
}