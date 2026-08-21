using System.Globalization;
using System.Text.Json;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.DataGrid;

namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

/// <summary>Helpers for reading file ids from projected query rows (dynamic JSON / Guid).</summary>
public static class FileStorageGridRowHelper
{
    /// <summary>QueryProject condition for rows that are not tombstones (<c>DeletedAt</c> is null and availability is not Deleted).</summary>
    public static WhereClause CreateActiveFilesWhere()
        => WhereClauseBuilder.And(static b => b.Equals("DeletedAt", null).NotEquals("Availability", nameof(FileAvailability.Deleted)));

    /// <summary>
    /// AND-combines <see cref="CreateActiveFilesWhere" /> onto <paramref name="query" /> so tombstones stay out of File and Tree listings.
    /// Does not drop search or column filters already on the builder.
    /// </summary>
    public static void ExcludeDeleted(ProjectionQueryReqBuilder query)
    {
        ArgumentHelpers.ThrowIfNull(query);
        var existing = query.Build().WhereClause;
        var active = CreateActiveFilesWhere();
        query.AddWhere(existing == null ? active : WhereClauseBuilder.CombineAs(GroupOperatorEnum.And, existing, active));
    }

    /// <summary>Returns whether the projected row represents a soft-deleted metadata tombstone.</summary>
    public static bool IsRowDeleted(object? row)
    {
        if (!string.IsNullOrWhiteSpace(GetDeletedAtDisplay(row)))
            return true;

        var availability = ProjectedValueHelper.GetDisplayValue(row, "Availability");
        return string.Equals(availability, nameof(FileAvailability.Deleted), StringComparison.OrdinalIgnoreCase);
    }

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

    /// <summary>Reads <c>PathPrefix</c> from a projected row, treating blank as unset (default shard layout).</summary>
    public static string? GetPathPrefixFromRow(object? row)
    {
        var display = ProjectedValueHelper.GetDisplayValue(row, "PathPrefix");
        return string.IsNullOrWhiteSpace(display) ? null : display;
    }

    /// <summary>Reads <c>SourceFileName</c> / stored object name from a projected row.</summary>
    public static string GetSourceFileNameFromRow(object? row) => ProjectedValueHelper.GetDisplayValue(row, "SourceFileName");

    /// <summary>Reads <c>OriginalFileName</c> from a projected row.</summary>
    public static string GetOriginalFileNameFromRow(object? row) => ProjectedValueHelper.GetDisplayValue(row, "OriginalFileName");

    /// <summary>Expected backend object key from metadata fields on the projected row (no global storage prefix).</summary>
    public static string? GetExpectedStorageKey(object? row)
    {
        if (!TryGetFileIdFromRow(row, out var fileId))
            return null;

        return BuildExpectedStorageKey(fileId, GetSourceFileNameFromRow(row), GetPathPrefixFromRow(row));
    }

    /// <summary>Same layout as the storage engine: <c>[pathPrefix or shard]/{fileId:N}{suffix}</c>, with no global storage prefix.</summary>
    private static string BuildExpectedStorageKey(Guid fileId, string? sourceFileName, string? pathPrefix)
    {
        var idString = fileId.ToString("N");
        var suffix = InferTrailingSuffixAfterFileId(fileId, sourceFileName);
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(pathPrefix))
            parts.Add(pathPrefix);
        else {
            parts.Add(idString[..2]);
            parts.Add(idString.Substring(2, 2));
        }

        parts.Add(idString + suffix);
        return string.Join("/", parts);
    }

    private static string InferTrailingSuffixAfterFileId(Guid fileId, string? sourceFileName)
    {
        if (string.IsNullOrEmpty(sourceFileName))
            return "";

        var n = fileId.ToString("N");
        if (sourceFileName.StartsWith(n, StringComparison.Ordinal))
            return sourceFileName[n.Length..];

        var dash = fileId.ToString();
        return sourceFileName.StartsWith(dash, StringComparison.OrdinalIgnoreCase) ? sourceFileName[dash.Length..] : "";
    }
}