using System.Diagnostics;
using LyoDataTable = Lyo.DataTable.Models.DataTable;

namespace Lyo.FileStorage.Web.Components.FileMetadata;

/// <summary>One sheet in <see cref="FileTabularPreviewDialog" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileTabularPreviewSheet(string Title, LyoDataTable Table)
{
    /// <inheritdoc />
    public override string ToString() => $"FileTabularPreviewSheet: Title={Title}, Rows={Table.Rows.Count}";
}
