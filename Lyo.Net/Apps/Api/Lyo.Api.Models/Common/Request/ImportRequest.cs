using Lyo.Api.Models.Enums;

namespace Lyo.Api.Models.Common.Request;

/// <summary>File import. The host saves the returned rows. This request does not name a store.</summary>
public sealed class ImportRequest
{
    /// <summary>Format handler to run.</summary>
    public ImportFormat Format { get; set; }

    /// <summary>JSON shape. Ignored for other formats. Starts as <see cref="ImportJsonMode.Array" />.</summary>
    public ImportJsonMode JsonMode { get; set; } = ImportJsonMode.Array;

    /// <summary>Array property on a wrapped JSON object, such as <c>items</c>.</summary>
    public string? RowsPath { get; set; }

    /// <summary>Worksheet name. Null uses the first sheet.</summary>
    public string? SheetName { get; set; }

    /// <summary>Text body for JSON, CSV, YAML, XML, and TOML. XLSX may send base64 here when <see cref="ContentBytes" /> is null.</summary>
    public string? Content { get; set; }

    /// <summary>Workbook bytes. Used for XLSX.</summary>
    public byte[]? ContentBytes { get; set; }

    /// <summary>Source-to-target map applied after the file is converted to JSON. Empty means bind names as they are.</summary>
    public List<ImportFieldMap>? Fields { get; set; }
}
