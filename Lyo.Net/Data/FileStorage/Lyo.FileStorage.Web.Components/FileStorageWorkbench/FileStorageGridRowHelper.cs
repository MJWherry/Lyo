using System.Text.Json;
using Lyo.Web.Components.DataGrid;

namespace Lyo.FileStorage.Web.Components;

/// <summary>Helpers for reading file ids from projected query rows (dynamic JSON / Guid).</summary>
public static class FileStorageGridRowHelper
{
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

    public static object[] GetFileRowKey(object? item)
        => TryGetFileIdFromRow(item, out var id) ? [id.ToString()] : [];
}
